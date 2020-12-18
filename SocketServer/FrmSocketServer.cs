using Autofac;
using Contract;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autofac.Extras.DynamicProxy;
using System.Diagnostics;

namespace SocketServer
{
    public partial class FrmSocketServer : Form
    {
        private int _serverPort = Convert.ToInt32(ConfigurationManager.AppSettings["ServerPort"]);
        private SocketServerHelper _socketServerHelper;
        private Random _rnd = new Random();

        public FrmSocketServer()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                StartSocketServer();
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
        /// 启动Socket服务端
        /// </summary>
        private void StartSocketServer()
        {
            Task.Factory.StartNew(() =>
            {
                _socketServerHelper = new SocketServerHelper(_serverPort);
                RequestUtil._socketServerHelper = _socketServerHelper;
                _socketServerHelper.StartServer();
                _socketServerHelper.SocketReceivedEvent += Received;
                _socketServerHelper.SocketClientRegisterEvent += ClientRegistered;
                Log("socket服务端启动成功");
            });
        }

        /// <summary>
        /// Socket客户端注册事件
        /// </summary>
        private void ClientRegistered(object sender, SocketClientRegisterEventArgs e)
        {
            Log("客户端 " + e.SocketClientId + " 已连接注册");
        }

        /// <summary>
        /// Socket消息接收、处理、反馈
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
                        Log("收到客户端数据：" + e.Content.Content);

                        //回调
                        SocketResult result = new SocketResult();
                        result.Success = true;
                        result.Msg = "消息已成功收到";
                        e.Callback.SendResult(_socketServerHelper, result, e);
                    }
                }
                catch (Exception ex)
                {
                    Log("错误：" + ex.Message);
                }
            });
        }

        /// <summary>
        /// 广播消息
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            Stopwatch stopwath = new Stopwatch();
            stopwath.Start();

            //创建代理
            IMyTest proxy = ProxyFactory.CreateProxy<IMyTest>();

            //调用方法
            int arg1 = _rnd.Next(1, 10);
            int arg2 = _rnd.Next(1, 10);
            proxy.Calc(arg1, arg2);

            double d = stopwath.Elapsed.TotalSeconds;
            stopwath.Stop();
            string strTime = " 耗时：" + d.ToString("0.000000000") + " 秒";

            //输出结果
            Log(string.Format("计算完成{0} + {1} {2}", arg1, arg2, strTime));
        }

        /// <summary>
        /// 向指定客户端发送消息
        /// </summary>
        private void btnSendToClient_Click(object sender, EventArgs e)
        {

        }
    }
}
