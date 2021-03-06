﻿using Newtonsoft.Json;
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

namespace SocketServer
{
    public partial class FrmSocketServer : Form
    {
        private int _serverPort = Convert.ToInt32(ConfigurationManager.AppSettings["ServerPort"]);
        private SocketServerHelper _socketServerHelper;

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
            string msg = txtMsg.Text;

            foreach (string socketClientId in _socketServerHelper.GetSocketClientIdListAll())
            {
                Task.Factory.StartNew((obj) =>
                {
                    try
                    {
                        for (int i = 0; i < 1; i++)
                        {
                            string clientId = (string)obj;

                            MsgContent content = new MsgContent();
                            content.Content = msg;

                            _socketServerHelper.Send(content, clientId, (result) =>
                            {
                                if (result.Success)
                                {
                                    Log("收到客户端 " + clientId + " 成功反馈");
                                }
                                else
                                {
                                    Log("收到客户端 " + clientId + " 失败反馈，失败消息：" + result.Msg);
                                }
                            });
                            Log("向客户端 " + clientId + " 发送消息");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(ex.Message);
                    }
                }, socketClientId);
            }
        }

        /// <summary>
        /// 向指定客户端发送消息
        /// </summary>
        private void btnSendToClient_Click(object sender, EventArgs e)
        {
            string msg = txtMsg.Text;
            string clientId = txtClientId.Text;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    MsgContent content = new MsgContent();
                    content.Content = msg;

                    _socketServerHelper.Send(content, clientId, (result) =>
                    {
                        if (result.Success)
                        {
                            Log("收到客户端 " + clientId + " 成功反馈");
                        }
                        else
                        {
                            Log("收到客户端 " + clientId + " 失败反馈，失败消息：" + result.Msg);
                        }
                    });
                    Log("向客户端 " + clientId + " 发送消息");
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                }
            });
        }
    }
}
