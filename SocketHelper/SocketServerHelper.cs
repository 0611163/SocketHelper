using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// Socket服务端帮助类
    /// </summary>
    public class SocketServerHelper
    {
        #region 变量
        private int _serverPort;
        private Socket serverSocket;
        private ConcurrentDictionary<ClientSocket, string> clientSocketList = new ConcurrentDictionary<ClientSocket, string>();
        private ConcurrentDictionary<string, ClientSocket> _dictClientIdClientSocket = new ConcurrentDictionary<string, ClientSocket>();

        public int _CallbackTimeout = 20;
        /// <summary>
        /// 等待回调超时时间(单位：秒)
        /// </summary>
        public int CallbackTimeout
        {
            get { return _CallbackTimeout; }
            set { value = _CallbackTimeout; }
        }

        public int _WaitResultTimeout = 20;
        /// <summary>
        /// 等待返回结果超时时间(单位：秒)
        /// </summary>
        public int WaitResultTimeout
        {
            get { return _WaitResultTimeout; }
            set { value = _WaitResultTimeout; }
        }

        private object _lockSend = new object();

        public event EventHandler<ReceivedSocketResultEventArgs> ReceivedSocketResultEvent;

        public event EventHandler<SocketClientOfflineEventArgs> SocketClientOfflineEvent;

        public event EventHandler<SocketClientRegisterEventArgs> SocketClientRegisterEvent;

        public event EventHandler<SocketReceivedEventArgs> SocketReceivedEvent;

        private System.Timers.Timer _checkClientTimer;
        #endregion

        #region SocketServerHelper 构造函数
        public SocketServerHelper(int serverPort)
        {
            _serverPort = serverPort;
        }
        #endregion

        #region 启动服务
        /// <summary>
        /// 启动服务
        /// </summary>
        public bool StartServer()
        {
            try
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, _serverPort);
                serverSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(ipEndPoint);
                serverSocket.Listen(5000);
                Thread thread = new Thread(new ThreadStart(delegate ()
                {
                    while (true)
                    {
                        Socket client = null;
                        ClientSocket clientSocket = null;

                        try
                        {
                            client = serverSocket.Accept();
                            client.SendTimeout = 20000;
                            client.ReceiveTimeout = 20000;
                            client.SendBufferSize = 10240;
                            client.ReceiveBufferSize = 10240;
                            clientSocket = new ClientSocket(client);
                            clientSocketList.TryAdd(clientSocket, null);
                            LogUtil.Log("监听到新的客户端，当前客户端数：" + clientSocketList.Count);
                        }
                        catch (Exception ex)
                        {
                            LogUtil.Error(ex);
                            Thread.Sleep(1);
                            continue;
                        }

                        if (client == null) continue;

                        try
                        {
                            byte[] buffer = new byte[10240];
                            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                            clientSocket.SocketAsyncArgs = args;
                            clientSocket.SocketAsyncCompleted = (s, e) =>
                            {
                                ReceiveData(clientSocket, e);
                            };
                            args.SetBuffer(buffer, 0, buffer.Length);
                            args.Completed += clientSocket.SocketAsyncCompleted;
                            client.ReceiveAsync(args);
                        }
                        catch (Exception ex)
                        {
                            LogUtil.Error(ex);
                        }
                    }
                }));
                thread.IsBackground = true;
                thread.Start();

                //检测客户端
                _checkClientTimer = new System.Timers.Timer();
                _checkClientTimer.AutoReset = false;
                _checkClientTimer.Interval = 1000;
                _checkClientTimer.Elapsed += CheckClient;
                _checkClientTimer.Start();

                LogUtil.Log("服务已启动");
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "启动服务出错");
                return false;
            }
        }
        #endregion

        #region 检测客户端
        /// <summary>
        /// 检测客户端
        /// </summary>
        private void CheckClient(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                foreach (ClientSocket clientSkt in clientSocketList.Keys.ToArray())
                {
                    Socket skt = clientSkt.Socket;
                    ClientSocket temp;
                    string strTemp;

                    DateTime now = DateTime.Now;
                    if (now.Subtract(clientSkt.LastHeartbeat).TotalSeconds > 60)
                    {
                        clientSocketList.TryRemove(clientSkt, out strTemp);
                        if (clientSkt.SocketClientId != null && SocketClientOfflineEvent != null)
                        {
                            SocketClientOfflineEventArgs socketClientOfflineEventArgs = new SocketClientOfflineEventArgs(clientSkt.SocketClientId);
                            SocketClientOfflineEvent(null, socketClientOfflineEventArgs);
                        }
                        LogUtil.Log("客户端已失去连接，当前客户端数：" + clientSocketList.Count);
                        ActionUtil.TryDoAction(() => { if (skt.Connected) skt.Disconnect(false); });
                        ActionUtil.TryDoAction(() =>
                        {
                            skt.Close();
                            skt.Dispose();
                            if (clientSkt.SocketAsyncArgs != null)
                            {
                                if (clientSkt.SocketAsyncCompleted != null)
                                {
                                    clientSkt.SocketAsyncArgs.Completed -= clientSkt.SocketAsyncCompleted;
                                }
                                clientSkt.SocketAsyncArgs.Dispose();
                            }
                            clientSkt.SocketAsyncCompleted = null;
                            clientSkt.SocketAsyncArgs = null;
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "检测客户端出错");
            }
            finally
            {
                _checkClientTimer.Start();
            }
        }
        #endregion

        #region 接收数据
        /// <summary>
        /// 处理接收的数据包
        /// </summary>
        private void ReceiveData(ClientSocket clientSkt, SocketAsyncEventArgs e)
        {
            if (clientSkt == null) return;
            Socket skt = clientSkt.Socket;

            try
            {
                CopyTo(e.Buffer, clientSkt.Buffer, 0, e.BytesTransferred);

                #region 校验数据
                if (clientSkt.Buffer.Count < 4)
                {
                    if (skt.Connected)
                    {
                        if (!skt.ReceiveAsync(e)) ReceiveData(clientSkt, e);
                    }
                    return;
                }
                else
                {
                    byte[] bArrHeader = new byte[4];
                    CopyTo(clientSkt.Buffer, bArrHeader, 0, 0, bArrHeader.Length);
                    string strHeader = Encoding.ASCII.GetString(bArrHeader);
                    if (strHeader.ToUpper() == SocketData.HeaderString)
                    {
                        if (clientSkt.Buffer.Count < 5)
                        {
                            if (skt.Connected)
                            {
                                if (!skt.ReceiveAsync(e)) ReceiveData(clientSkt, e);
                            }
                            return;
                        }
                        else
                        {
                            byte[] bArrType = new byte[1];
                            CopyTo(clientSkt.Buffer, bArrType, 4, 0, bArrType.Length);
                            if (bArrType[0] == (int)SocketDataType.心跳) { } //心跳包
                            else if (bArrType[0] == (int)SocketDataType.注册 || bArrType[0] == (int)SocketDataType.消息数据 || bArrType[0] == (int)SocketDataType.返回值) //注册包、消息数据、返回值包
                            {
                                if (clientSkt.Buffer.Count < 9)
                                {
                                    if (skt.Connected)
                                    {
                                        if (!skt.ReceiveAsync(e)) ReceiveData(clientSkt, e);
                                    }
                                    return;
                                }
                                else
                                {
                                    byte[] bArrLength = new byte[4];
                                    CopyTo(clientSkt.Buffer, bArrLength, 5, 0, bArrLength.Length);
                                    int dataLength = BitConverter.ToInt32(bArrLength, 0);
                                    if (dataLength == 0 || clientSkt.Buffer.Count < dataLength + 9)
                                    {
                                        if (skt.Connected)
                                        {
                                            if (!skt.ReceiveAsync(e)) ReceiveData(clientSkt, e);
                                        }
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                LogUtil.Error(string.Format("type错误，丢掉错误数据，重新接收，SocketClientId={0}", clientSkt.SocketClientId));
                                clientSkt.Buffer.Clear(); //把错误的数据丢掉
                                if (skt.Connected)
                                {
                                    if (!skt.ReceiveAsync(e)) ReceiveData(clientSkt, e);
                                }
                                return;
                            }
                        }
                    }
                    else
                    {
                        LogUtil.Error(string.Format("不是" + SocketData.HeaderString + "，丢掉错误数据，重新接收，SocketClientId={0}", clientSkt.SocketClientId));
                        LogUtil.Error(ByteArrToString(clientSkt.Buffer));
                        clientSkt.Buffer.Clear(); //把错误的数据丢掉
                        if (skt.Connected)
                        {
                            if (!skt.ReceiveAsync(e)) ReceiveData(clientSkt, e);
                        }
                        return;
                    }
                }
                #endregion

                SocketData data = null;
                do
                {
                    data = ProcessSocketData(clientSkt);
                } while (data != null);

                if (skt.Connected)
                {
                    if (!skt.ReceiveAsync(e)) ReceiveData(clientSkt, e);
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "处理接收的数据包 异常");
            }
        }

        /// <summary>
        /// 字节数组转字符串
        /// </summary>
        private string ByteArrToString(List<byte> byteList)
        {
            List<string> list = new List<string>();

            foreach (byte b in byteList)
            {
                list.Add(b.ToString("X2"));
            }

            return string.Join("  ", list);
        }
        #endregion

        #region 处理接收的数据包
        /// <summary>
        /// 处理接收的数据包
        /// </summary>
        private SocketData ProcessSocketData(ClientSocket clientSkt)
        {
            int readLength = 0;
            SocketData data = ResolveBuffer(clientSkt.Buffer, out readLength);
            if (data != null)
            {
                if (readLength > 0) clientSkt.RemoveBufferData(readLength);
                if (data.Type == SocketDataType.心跳) //收到心跳包
                {
                    clientSkt.LastHeartbeat = DateTime.Now;

                    //心跳应答
                    if (clientSkt.SocketClientId != null)
                    {
                        lock (clientSkt.LockSend)
                        {
                            List<byte> byteList = new List<byte>();
                            byte[] bArrHeader = Encoding.ASCII.GetBytes(SocketData.HeaderString);
                            ByteUtil.Append(ref byteList, bArrHeader);
                            ByteUtil.Append(ref byteList, new byte[] { (byte)SocketDataType.心跳应答 });
                            SocketHelper.Send(clientSkt.Socket, byteList.ToArray());
                        }
                    }
                    else
                    {
                        LogUtil.Log("没有注册信息");
                    }

                    //LogUtil.Log("收到心跳包，客户端连接正常，roomNo=" + clientSkt.RoomNo + "，devNo=" + clientSkt.DevNo);
                }

                if (data.Type == SocketDataType.注册) //收到注册包
                {
                    if (data.SocketRegisterData != null && clientSkt != null)
                    {
                        ClientSocket temp;
                        if (data.SocketRegisterData.SocketClientId != null) _dictClientIdClientSocket.TryRemove(data.SocketRegisterData.SocketClientId, out temp);
                        clientSkt.SocketClientId = data.SocketRegisterData.SocketClientId;
                        if (data.SocketRegisterData.SocketClientId != null) _dictClientIdClientSocket.TryAdd(data.SocketRegisterData.SocketClientId, clientSkt);
                        LogUtil.Log("收到注册包，SocketClientId=" + clientSkt.SocketClientId);

                        //更新仓内屏设备的数据库状态
                        if (data.SocketRegisterData.SocketClientId != null && SocketClientRegisterEvent != null)
                        {
                            SocketClientRegisterEvent(null, new SocketClientRegisterEventArgs(data.SocketRegisterData.SocketClientId));
                        }

                        //注册反馈
                        lock (clientSkt.LockSend)
                        {
                            List<byte> byteList = new List<byte>();
                            byte[] bArrHeader = Encoding.ASCII.GetBytes(SocketData.HeaderString);
                            ByteUtil.Append(ref byteList, bArrHeader);
                            ByteUtil.Append(ref byteList, new byte[] { (byte)SocketDataType.注册反馈 });
                            SocketHelper.Send(clientSkt.Socket, byteList.ToArray());
                        }
                    }
                }

                if (data.Type == SocketDataType.返回值) //收到返回值包
                {
                    ThreadHelper.Run(() =>
                    {
                        if (data.SocketResult != null) clientSkt.CallbackDict.TryAdd(data.SocketResult.callbackId, data.SocketResult);

                        if (ReceivedSocketResultEvent != null)
                        {
                            ReceivedSocketResultEvent(null, new ReceivedSocketResultEventArgs(data.SocketResult));
                        }
                    });

                    //LogUtil.Log("收到返回值包，SocketClientId=" + clientSkt.SocketClientId);
                }

                if (data.Type == SocketDataType.消息数据 && SocketReceivedEvent != null) //收到消息数据
                {
                    SocketReceivedEventArgs socketReceivedEventArgs = new SocketReceivedEventArgs(data.Content);
                    socketReceivedEventArgs.Callback = new CallbackSocket(clientSkt);
                    SocketReceivedEvent(null, socketReceivedEventArgs);
                }
            }
            return data;
        }
        #endregion

        #region ResolveBuffer
        /// <summary>
        /// 解析字节数组
        /// </summary>
        private SocketData ResolveBuffer(List<byte> buffer, out int readLength)
        {
            SocketData socketData = null;
            readLength = 0;

            try
            {
                if (buffer.Count < 4) return null;
                byte[] bArrHeader = new byte[4];
                CopyTo(buffer, bArrHeader, 0, 0, bArrHeader.Length);
                readLength += bArrHeader.Length;
                string strHeader = Encoding.ASCII.GetString(bArrHeader);
                if (strHeader.ToUpper() == SocketData.HeaderString)
                {
                    if (buffer.Count < 5) return null;
                    byte[] bArrType = new byte[1];
                    CopyTo(buffer, bArrType, 4, 0, bArrType.Length);
                    readLength += bArrType.Length;
                    byte bType = bArrType[0];
                    socketData = new SocketData();
                    socketData.Type = (SocketDataType)bType;

                    if (socketData.Type == SocketDataType.注册)
                    {
                        if (buffer.Count < 9) return null;
                        byte[] bArrLength = new byte[4];
                        CopyTo(buffer, bArrLength, 5, 0, bArrLength.Length);
                        readLength += bArrLength.Length;
                        int dataLength = BitConverter.ToInt32(bArrLength, 0);

                        if (dataLength == 0 || buffer.Count < dataLength + 9) return null;
                        byte[] dataBody = new byte[dataLength];
                        CopyTo(buffer, dataBody, 9, 0, dataBody.Length);
                        readLength += dataBody.Length;
                        string jsonString = Encoding.UTF8.GetString(dataBody);
                        socketData.SocketRegisterData = JsonConvert.DeserializeObject<SocketRegisterData>(jsonString);
                    }

                    if (socketData.Type == SocketDataType.返回值)
                    {
                        if (buffer.Count < 9) return null;
                        byte[] bArrLength = new byte[4];
                        CopyTo(buffer, bArrLength, 5, 0, bArrLength.Length);
                        readLength += bArrLength.Length;
                        int dataLength = BitConverter.ToInt32(bArrLength, 0);

                        if (dataLength == 0 || buffer.Count < dataLength + 9) return null;
                        byte[] dataBody = new byte[dataLength];
                        CopyTo(buffer, dataBody, 9, 0, dataBody.Length);
                        readLength += dataBody.Length;
                        string jsonString = Encoding.UTF8.GetString(dataBody);
                        socketData.SocketResult = JsonConvert.DeserializeObject<SocketResult>(jsonString);
                    }

                    if (socketData.Type == SocketDataType.消息数据)
                    {
                        if (buffer.Count < 9) return null;
                        byte[] bArrLength = new byte[4];
                        CopyTo(buffer, bArrLength, 5, 0, bArrLength.Length);
                        readLength += bArrLength.Length;
                        int dataLength = BitConverter.ToInt32(bArrLength, 0);

                        if (dataLength == 0 || buffer.Count < dataLength + 9) return null;
                        byte[] dataBody = new byte[dataLength];
                        CopyTo(buffer, dataBody, 9, 0, dataBody.Length);
                        readLength += dataBody.Length;
                        string jsonString = Encoding.UTF8.GetString(dataBody);
                        socketData.Content = JsonConvert.DeserializeObject<MsgContent>(jsonString);
                    }
                }
                else
                {
                    LogUtil.Error("不是" + SocketData.HeaderString);
                    return null;
                }

            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "解析字节数组 出错");
                return null;
            }

            return socketData;
        }
        #endregion

        #region CopyTo
        /// <summary>
        /// 数组复制
        /// </summary>
        private void CopyTo(byte[] bArrSource, List<byte> listTarget, int sourceIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (sourceIndex + i < bArrSource.Length)
                {
                    listTarget.Add(bArrSource[sourceIndex + i]);
                }
            }
        }

        /// <summary>
        /// 数组复制
        /// </summary>
        private void CopyTo(List<byte> listSource, byte[] bArrTarget, int sourceIndex, int targetIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (targetIndex + i < bArrTarget.Length && sourceIndex + i < listSource.Count)
                {
                    bArrTarget[targetIndex + i] = listSource[sourceIndex + i];
                }
            }
        }
        #endregion

        #region 停止服务
        /// <summary>
        /// 停止服务
        /// </summary>
        public void StopServer()
        {
            try
            {
                foreach (ClientSocket clientSocket in clientSocketList.Keys.ToArray())
                {
                    Socket socket = clientSocket.Socket;
                    ActionUtil.TryDoAction(() => { if (socket.Connected) socket.Disconnect(false); });
                    ActionUtil.TryDoAction(() =>
                    {
                        socket.Close();
                        socket.Dispose();
                    });
                }
                clientSocketList.Clear();
                _dictClientIdClientSocket.Clear();
                if (serverSocket != null)
                {
                    ActionUtil.TryDoAction(() => { if (serverSocket.Connected) serverSocket.Disconnect(false); });
                    ActionUtil.TryDoAction(() =>
                    {
                        serverSocket.Close();
                        serverSocket.Dispose();
                    });
                }
                LogUtil.Log("服务已停止");
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "停止服务出错");
            }
        }
        #endregion

        #region 释放资源
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_checkClientTimer != null)
            {
                _checkClientTimer.Stop();
                _checkClientTimer.Close();
            }
        }
        #endregion

        #region Send
        /// <summary>
        /// Send 单个发送 并等待结果
        /// </summary>
        /// <returns>false：发送失败 true：发送成功，但接收端是否处理成功要等待返回结果</returns>
        public SocketResult Send(MsgContent content, string socketClientId)
        {
            SocketData data = new SocketData();
            data.Type = SocketDataType.消息数据;
            data.Content = content;

            ClientSocket clientSocket = null;
            if (socketClientId != null) _dictClientIdClientSocket.TryGetValue(socketClientId, out clientSocket);

            if (clientSocket != null)
            {
                Send(clientSocket, data);
                return WaitSocketResult(clientSocket, content.CallbackId);
            }
            else
            {
                SocketResult socketResult = new SocketResult();
                socketResult.success = false;
                socketResult.errorMsg = "客户端不存在";
                return socketResult;
            }
        }

        /// <summary>
        /// Send 单个发送
        /// </summary>
        /// <returns>false：发送失败 true：发送成功，但接收端是否处理成功要等待返回结果</returns>
        public void Send(MsgContent content, string socketClientId, Action<SocketResult> callback = null)
        {
            SocketData data = new SocketData();
            data.Type = SocketDataType.消息数据;
            data.Content = content;

            ClientSocket clientSocket = null;
            if (socketClientId != null) _dictClientIdClientSocket.TryGetValue(socketClientId, out clientSocket);

            if (clientSocket != null)
            {
                if (callback != null)
                {
                    WaitCallback(clientSocket, content.CallbackId, callback);
                }

                Send(clientSocket, data);
            }
            else
            {
                SocketResult socketResult = new SocketResult();
                socketResult.success = false;
                socketResult.errorMsg = "客户端不存在";
                if (callback != null) callback(socketResult);
            }
        }

        /// <summary>
        /// 等待回调
        /// </summary>
        private void WaitCallback(ClientSocket clientSocket, string callbackId, Action<SocketResult> callback = null)
        {
            DateTime dt = DateTime.Now.AddSeconds(_CallbackTimeout);
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.AutoReset = false;
            timer.Interval = 100;
            timer.Elapsed += (s, e) =>
            {
                try
                {
                    SocketResult socketResult;
                    if (!clientSocket.CallbackDict.TryGetValue(callbackId, out socketResult) && DateTime.Now < dt)
                    {
                        timer.Start();
                        return;
                    }
                    SocketResult sktResult;
                    clientSocket.CallbackDict.TryRemove(callbackId, out sktResult);
                    if (socketResult == null)
                    {
                        socketResult = new SocketResult();
                        socketResult.success = false;
                        socketResult.errorMsg = "超时";
                    }

                    if (callback != null) callback(socketResult);

                    timer.Close();
                }
                catch (Exception ex)
                {
                    LogUtil.Error("WaitCallback error" + ex);
                }
            };
            timer.Start();
        }

        /// <summary>
        /// 等待SocketResult
        /// </summary>
        private SocketResult WaitSocketResult(ClientSocket clientSocket, string callbackId)
        {
            SocketResult socketResult;
            DateTime dt = DateTime.Now.AddSeconds(_WaitResultTimeout);
            while (!clientSocket.CallbackDict.TryGetValue(callbackId, out socketResult) && DateTime.Now < dt)
            {
                Thread.Sleep(10);
            }
            SocketResult sktResult;
            clientSocket.CallbackDict.TryRemove(callbackId, out sktResult);
            if (socketResult == null)
            {
                socketResult = new SocketResult();
                socketResult.success = false;
                socketResult.errorMsg = "超时";
            }
            return socketResult;
        }

        /// <summary>
        /// Send
        /// </summary>
        /// <returns>false：发送失败 true：发送成功，但不表示对方已收到</returns>
        public void Send(ClientSocket clientSocket, SocketData data)
        {
            Socket socket = clientSocket.Socket;

            List<byte> byteList = new List<byte>();
            byte[] bArrHeader = Encoding.ASCII.GetBytes(SocketData.HeaderString); //header
            ByteUtil.Append(ref byteList, bArrHeader);

            if (data.Type == SocketDataType.心跳应答)
            {
                ByteUtil.Append(ref byteList, new byte[] { (byte)SocketDataType.心跳应答 }); //type
            }

            if (data.Type == SocketDataType.消息数据)
            {
                ByteUtil.Append(ref byteList, new byte[] { (byte)SocketDataType.消息数据 }); //type

                if (data.Content != null)
                {
                    byte[] bArrData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data.Content));
                    ByteUtil.Append(ref byteList, BitConverter.GetBytes(bArrData.Length)); //发送length
                    ByteUtil.Append(ref byteList, bArrData); //发送body
                }
            }

            if (data.Type == SocketDataType.返回值)
            {
                ByteUtil.Append(ref byteList, new byte[] { (byte)SocketDataType.返回值 }); //type

                if (data.SocketResult != null)
                {
                    byte[] bArrData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data.SocketResult));
                    ByteUtil.Append(ref byteList, BitConverter.GetBytes(bArrData.Length)); //发送length
                    ByteUtil.Append(ref byteList, bArrData); //发送body
                }
            }

            lock (clientSocket.LockSend)
            {
                SocketHelper.Send(socket, byteList.ToArray()); //发送
            }
        }
        #endregion

    }

}
