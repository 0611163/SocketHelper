using Newtonsoft.Json;
using SocketUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketClient
{
    public partial class FrmSocketClient : Form
    {
        private string _serverIP = ConfigurationManager.AppSettings["ServerIP"];
        private int _serverPort = Convert.ToInt32(ConfigurationManager.AppSettings["ServerPort"]);
        private SocketClientHelper _socketClientHelper;

        public FrmSocketClient()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                StartSocketClient();
            });
        }

        /// <summary>
        /// 启动Socket客户端
        /// </summary>
        private void StartSocketClient()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _socketClientHelper = new SocketClientHelper(_serverIP, _serverPort);
                    _socketClientHelper.ConnectServer(); //连接服务器
                    _socketClientHelper.StartHeartbeat(); //心跳
                    _socketClientHelper.RegisterToServer(string.IsNullOrWhiteSpace(txtSocketClientId.Text) ? Guid.NewGuid().ToString("N") : txtSocketClientId.Text); //注册

                    _socketClientHelper.SocketReceivedEvent += Received;
                    _socketClientHelper.ReceivedSocketResultEvent += ResultReceived;
                    Log("客户端连接服务端成功");
                }
                catch (Exception ex)
                {
                    LogUtil.Error(ex);
                }
            });
        }

        /// <summary>
        /// Socket数据接收
        /// </summary>
        private void Received(object sender, SocketReceivedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    //取数据处理
                    if (e.Content != null)
                    {
                        Log("收到服务端消息：" + e.Content.Content);

                        //回调
                        SocketResult result = new SocketResult();
                        result.success = true;
                        result.errorMsg = "消息已成功收到";
                        e.Callback.SendResult(_socketClientHelper, result, e);
                    }
                }
                catch (Exception ex)
                {
                    Log("错误：" + ex.Message);
                }
            });
        }

        /// <summary>
        /// 反馈消息接收
        /// </summary>
        private void ResultReceived(object sender, ReceivedSocketResultEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    //取数据处理
                    if (e.SocketResult != null)
                    {
                        Log("收到反馈，反馈标识：" + e.SocketResult.callbackId);
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Error(ex);
                }
            });
        }

        #region Log
        /// <summary>
        /// 输出日志
        /// </summary>
        private void Log(string log)
        {
            if (!this.IsDisposed)
            {
                this.BeginInvoke(new Action(() =>
                {
                    txtLog.AppendText(DateTime.Now.ToString("mm:ss.fff") + " " + log + "\r\n\r\n");
                }));
            }
        }
        #endregion

        /// <summary>
        /// 向服务端发送消息
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            string msg = txtMsg.Text;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    MsgContent content = new MsgContent();
                    content.Content = msg;

                    SocketData data = new SocketData();
                    data.Type = SocketDataType.消息数据;
                    data.Content = content;

                    _socketClientHelper.Send(data, (result) =>
                    {
                        if (result.success)
                        {
                            Log("服务端发来成功反馈");
                        }
                        else
                        {
                            Log("服务端发来失败反馈，失败消息：" + result.errorMsg);
                        }
                    });
                    Log("向服务端发送消息");
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                }
            });
        }
    }
}
