using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// Socket客户端帮助类
    /// </summary>
    public class SocketClientHelper
    {
        #region 变量
        private string _serverIP;
        private string _hostName;
        private int _serverPort;
        private object _lockSend = new object();
        private Socket clientSocket;
        private SocketAsyncEventArgs _socketAsyncArgs;
        public EventHandler<SocketAsyncEventArgs> _socketAsyncCompleted { get; set; }
        private System.Timers.Timer heartbeatTimer;
        public event EventHandler<SocketReceivedEventArgs> SocketReceivedEvent;
        public event EventHandler<ReceivedSocketResultEventArgs> ReceivedSocketResultEvent;
        private System.Timers.Timer _checkServerTimer;
        private DateTime _lastHeartbeat;
        private List<byte> _buffer = new List<byte>();
        private string _clientId;
        private bool _registerSuccess = false;

        public string ClientId
        {
            get { return _clientId; }
        }

        public int _CallbackTimeout = 20;
        /// <summary>
        /// 等待回调超时时间(单位：秒)
        /// </summary>
        public int CallbackTimeout
        {
            get { return _CallbackTimeout; }
            set { value = _CallbackTimeout; }
        }

        /// <summary>
        /// 服务端操作结果回调
        /// </summary>
        private ConcurrentDictionary<string, SocketResult> _callbackDict = new ConcurrentDictionary<string, SocketResult>();

        #endregion

        #region SocketClientHelper 构造函数
        public SocketClientHelper(string serverIP, int serverPort, string hostName = null)
        {
            _serverIP = serverIP;
            _serverPort = serverPort;
            _hostName = hostName;
        }
        #endregion

        #region 连接服务器
        /// <summary>
        /// 连接服务器
        /// </summary>
        public bool ConnectServer()
        {
            try
            {
                if (clientSocket == null || !clientSocket.Connected)
                {
                    if (clientSocket != null)
                    {
                        clientSocket.Close();
                        clientSocket.Dispose();
                    }
                    IPEndPoint ipep = null;
                    if (_hostName != null)
                    {
                        IPHostEntry host = Dns.GetHostEntry(_hostName);
                        IPAddress ipAddr = host.AddressList[0];
                        ipep = new IPEndPoint(ipAddr, _serverPort);
                    }
                    else
                    {
                        ipep = new IPEndPoint(IPAddress.Parse(_serverIP), _serverPort);
                    }
                    clientSocket = new Socket(ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    clientSocket.SendTimeout = 20000;
                    clientSocket.ReceiveTimeout = 20000;
                    clientSocket.SendBufferSize = 10240;
                    clientSocket.ReceiveBufferSize = 10240;

                    try
                    {
                        clientSocket.Connect(ipep);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error(ex);
                        return false;
                    }

                    if (clientSocket == null || !clientSocket.Connected) return false;

                    _lastHeartbeat = DateTime.Now;

                    try
                    {
                        byte[] buffer = new byte[10240];
                        _socketAsyncArgs = new SocketAsyncEventArgs();
                        _socketAsyncArgs.SetBuffer(buffer, 0, buffer.Length);
                        _socketAsyncCompleted = (s, e) =>
                        {
                            ReceiveData(clientSocket, e);
                        };
                        _socketAsyncArgs.Completed += _socketAsyncCompleted;
                        clientSocket.ReceiveAsync(_socketAsyncArgs);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error(ex);
                    }

                    //检测服务端
                    _checkServerTimer = new System.Timers.Timer();
                    _checkServerTimer.AutoReset = false;
                    _checkServerTimer.Interval = 1000;
                    _checkServerTimer.Elapsed += CheckServer;
                    _checkServerTimer.Start();

                    LogUtil.Log("已连接服务器");
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "连接服务器失败");
                return false;
            }
        }
        #endregion

        #region 检测服务端
        /// <summary>
        /// 检测服务端
        /// </summary>
        private void CheckServer(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                DateTime now = DateTime.Now;
                if (now.Subtract(_lastHeartbeat).TotalSeconds > 15)
                {
                    LogUtil.Log("服务端已失去连接");
                    try
                    {
                        if (clientSocket.Connected) clientSocket.Disconnect(false);
                        clientSocket.Close();
                        clientSocket.Dispose();
                        _socketAsyncArgs.Completed -= _socketAsyncCompleted;
                        _socketAsyncCompleted = null;
                        _socketAsyncArgs.Dispose();
                        _socketAsyncArgs = null;
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error(ex);
                    }

                    Thread.Sleep(3000);
                    int tryCount = 0;
                    while (!ConnectServer() && tryCount++ < 10000) //重连
                    {
                        Thread.Sleep(3000);
                    }
                    RegisterToServer(_clientId); //重新注册
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "检测服务端出错");
            }
            finally
            {
                _checkServerTimer.Start();
            }
        }
        #endregion

        #region 断开服务器
        /// <summary>
        /// 断开服务器
        /// </summary>
        public void DisconnectServer()
        {
            try
            {
                if (clientSocket != null)
                {
                    if (clientSocket.Connected) clientSocket.Disconnect(false);
                    clientSocket.Close();
                    clientSocket.Dispose();
                }
                LogUtil.Log("已断开服务器");
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "断开服务器失败");
            }
        }
        #endregion

        #region 释放资源 
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (heartbeatTimer != null)
            {
                heartbeatTimer.Stop();
                heartbeatTimer.Close();
            }
            if (_checkServerTimer != null)
            {
                _checkServerTimer.Stop();
                _checkServerTimer.Close();
            }
        }
        #endregion

        #region 心跳
        public void StartHeartbeat()
        {
            heartbeatTimer = new System.Timers.Timer();
            heartbeatTimer.AutoReset = false;
            heartbeatTimer.Interval = 10000;
            heartbeatTimer.Elapsed += new System.Timers.ElapsedEventHandler((obj, eea) =>
            {
                lock (_lockSend)
                {
                    try
                    {
                        List<byte> byteList = new List<byte>();
                        byte[] bArrHeader = Encoding.ASCII.GetBytes(SocketData.HeaderString);
                        ByteUtil.Append(ref byteList, bArrHeader);
                        ByteUtil.Append(ref byteList, new byte[] { (byte)SocketDataType.心跳 });
                        SocketHelper.Send(clientSocket, byteList.ToArray());
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error("向服务器发送心跳包出错：" + ex.Message);
                    }
                    finally
                    {
                        heartbeatTimer.Start();
                    }
                }
            });
            heartbeatTimer.Start();
        }
        #endregion

        #region 停止心跳
        public void StopHeartbeat()
        {
            heartbeatTimer.Stop();
        }
        #endregion

        #region 注册
        /// <summary>
        /// 注册
        /// </summary>
        public bool RegisterToServer(string clientId)
        {
            _registerSuccess = false;
            SocketData data = new SocketData();
            data.Type = SocketDataType.注册;
            data.SocketRegisterData = new SocketRegisterData();
            data.SocketRegisterData.SocketClientId = clientId;
            _clientId = clientId;
            Send(data);

            DateTime dt = DateTime.Now;
            while (!_registerSuccess && DateTime.Now.Subtract(dt).TotalMilliseconds < 5000)
            {
                Thread.Sleep(100);
            }
            return _registerSuccess;
        }
        #endregion

        #region 接收数据
        /// <summary>
        /// 处理接收的数据包
        /// </summary>
        private void ReceiveData(Socket socket, SocketAsyncEventArgs e)
        {
            try
            {
                CopyTo(e.Buffer, _buffer, 0, e.BytesTransferred);

                #region 校验数据
                if (_buffer.Count < 4)
                {
                    if (socket.Connected)
                    {
                        if (!socket.ReceiveAsync(e)) ReceiveData(socket, e);
                    }
                    return;
                }
                else
                {
                    byte[] bArrHeader = new byte[4];
                    CopyTo(_buffer, bArrHeader, 0, 0, bArrHeader.Length);
                    string strHeader = Encoding.ASCII.GetString(bArrHeader);
                    if (strHeader.ToUpper() == SocketData.HeaderString)
                    {
                        if (_buffer.Count < 5)
                        {
                            if (socket.Connected)
                            {
                                if (!socket.ReceiveAsync(e)) ReceiveData(socket, e);
                            }
                            return;
                        }
                        else
                        {
                            byte[] bArrType = new byte[1];
                            CopyTo(_buffer, bArrType, 4, 0, bArrType.Length);
                            if (bArrType[0] == (byte)SocketDataType.心跳应答 || bArrType[0] == (byte)SocketDataType.注册反馈) { } //心跳应答包、注册反馈包
                            else if (bArrType[0] == (byte)SocketDataType.消息数据 || bArrType[0] == (byte)SocketDataType.返回值) //消息包、返回值包
                            {
                                if (_buffer.Count < 9)
                                {
                                    if (socket.Connected)
                                    {
                                        if (!socket.ReceiveAsync(e)) ReceiveData(socket, e);
                                    }
                                    return;
                                }
                                else
                                {
                                    byte[] bArrLength = new byte[4];
                                    CopyTo(_buffer, bArrLength, 5, 0, bArrLength.Length);
                                    int dataLength = BitConverter.ToInt32(bArrLength, 0);
                                    if (dataLength == 0 || _buffer.Count < dataLength + 9)
                                    {
                                        if (socket.Connected)
                                        {
                                            if (!socket.ReceiveAsync(e)) ReceiveData(socket, e);
                                        }
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                LogUtil.Error("type错误，丢掉错误数据，重新接收");
                                _buffer.Clear(); //把错误的数据丢掉
                                if (socket.Connected)
                                {
                                    if (!socket.ReceiveAsync(e)) ReceiveData(socket, e);
                                }
                                return;
                            }
                        }
                    }
                    else
                    {
                        LogUtil.Error("不是" + SocketData.HeaderString + "，丢掉错误数据，重新接收");
                        _buffer.Clear(); //把错误的数据丢掉
                        if (socket.Connected)
                        {
                            if (!socket.ReceiveAsync(e)) ReceiveData(socket, e);
                        }
                        return;
                    }
                }
                #endregion

                SocketData data = null;
                do
                {
                    data = ProcessSocketData(socket);
                } while (data != null);

                if (socket.Connected)
                {
                    if (!socket.ReceiveAsync(e)) ReceiveData(socket, e);
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "处理接收的数据包 异常");
            }
        }
        #endregion

        #region 处理接收的数据包
        /// <summary>
        /// 处理接收的数据包
        /// </summary>
        private SocketData ProcessSocketData(Socket socket)
        {
            int readLength = 0;
            SocketData data = ResolveBuffer(_buffer, out readLength);
            if (data != null)
            {
                if (readLength > 0) RemoveBufferData(readLength);
                if (data.Type == SocketDataType.心跳应答) //心跳应答
                {
                    _lastHeartbeat = DateTime.Now;
                    //LogUtil.Log("收到心跳应答包，服务端正常");
                }

                if (data.Type == SocketDataType.注册反馈) //注册反馈
                {
                    _registerSuccess = true;
                    LogUtil.Log("收到注册反馈包，注册成功");
                }

                if (data.Type == SocketDataType.消息数据) //消息数据
                {
                    if (SocketReceivedEvent != null)
                    {
                        SocketReceivedEventArgs args = new SocketReceivedEventArgs(data.Content);
                        args.Callback = new CallbackSocket(socket);
                        ThreadHelper.Run((obj) =>
                        {
                            try
                            {
                                SocketReceivedEvent(this, obj as SocketReceivedEventArgs);
                            }
                            catch (Exception ex)
                            {
                                LogUtil.Error(ex);
                            }
                        }, args);
                    }
                }

                if (data.Type == SocketDataType.返回值 && ReceivedSocketResultEvent != null) //收到返回值包
                {
                    _callbackDict.TryAdd(data.SocketResult.callbackId, data.SocketResult);

                    ReceivedSocketResultEvent(null, new ReceivedSocketResultEventArgs(data.SocketResult));
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

        #region RemoveBufferData
        /// <summary>
        /// 删除接收到的一个包
        /// </summary>
        private void RemoveBufferData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_buffer.Count > 0)
                {
                    _buffer.RemoveAt(0);
                }
            }
        }
        #endregion

        #region Send
        /// <summary>
        /// Send
        /// </summary>
        public void Send(SocketData data, Action<SocketResult> callback = null)
        {
            Send(clientSocket, data);

            if (callback != null)
            {
                WaitCallback(data.Content.CallbackId, callback);
            }
        }

        /// <summary>
        /// Send
        /// </summary>
        public void Send(Socket socket, SocketData data)
        {
            List<byte> byteList = new List<byte>();

            byte[] bArrHeader = Encoding.ASCII.GetBytes(SocketData.HeaderString); //header
            ByteUtil.Append(ref byteList, bArrHeader);

            if (data.Type == SocketDataType.注册)
            {
                ByteUtil.Append(ref byteList, new byte[] { (byte)SocketDataType.注册 }); //type

                if (data.SocketRegisterData != null)
                {
                    byte[] bArrData = null;
                    bArrData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data.SocketRegisterData));
                    ByteUtil.Append(ref byteList, BitConverter.GetBytes(bArrData.Length)); //length
                    ByteUtil.Append(ref byteList, bArrData); //body
                }
            }

            if (data.Type == SocketDataType.返回值)
            {
                ByteUtil.Append(ref byteList, new byte[] { (byte)SocketDataType.返回值 }); //type

                if (data.SocketResult != null)
                {
                    byte[] bArrData = null;
                    bArrData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data.SocketResult));
                    ByteUtil.Append(ref byteList, BitConverter.GetBytes(bArrData.Length)); //length
                    ByteUtil.Append(ref byteList, bArrData); //body
                }
            }

            if (data.Type == SocketDataType.消息数据)
            {
                ByteUtil.Append(ref byteList, new byte[] { (byte)SocketDataType.消息数据 }); //type

                if (data.Content != null)
                {
                    byte[] bArrData = null;
                    bArrData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data.Content));
                    ByteUtil.Append(ref byteList, BitConverter.GetBytes(bArrData.Length)); //length
                    ByteUtil.Append(ref byteList, bArrData); //body
                }
            }

            lock (_lockSend)
            {
                SocketHelper.Send(socket, byteList.ToArray()); //发送
            }
        }
        #endregion

        #region 等待回调
        /// <summary>
        /// 等待回调
        /// </summary>
        private void WaitCallback(string callbackId, Action<SocketResult> callback = null)
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
                    if (!_callbackDict.TryGetValue(callbackId, out socketResult) && DateTime.Now < dt)
                    {
                        timer.Start();
                        return;
                    }
                    SocketResult sktResult;
                    _callbackDict.TryRemove(callbackId, out sktResult);
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
        #endregion

    }
}
