using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 模拟网络请求类
    /// </summary>
    public class RequestUtil
    {
        public static SocketServerHelper _socketServerHelper { get; set; }

        /// <summary>
        /// 模拟网络请求
        /// </summary>
        /// <param name="socketClientId">Socket客户端ID</param>
        /// <param name="className">类名</param>
        /// <param name="methodName">实现方法名称</param>
        /// <param name="args">实现方法的参数集合</param>
        public static void DoRequest(string socketClientId, string className, string methodName, ParameterInfo[] parameterInfoArr, object[] args)
        {
            //准备POST数据
            RpcData rpcData = new RpcData();
            rpcData.interfaceName = className;
            rpcData.methodName = methodName;
            rpcData.param = parameterInfoArr;
            rpcData.paramValue = args;
            string strJson = JsonConvert.SerializeObject(rpcData);

            //网络请求
            if (socketClientId == null) //广播
            {
                foreach (string sktClientId in _socketServerHelper.GetSocketClientIdListAll())
                {
                    try
                    {
                        Send(sktClientId, strJson);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error(ex);
                    }
                }
            }
            else //定向
            {
                try
                {
                    Send(socketClientId, strJson);
                }
                catch (Exception ex)
                {
                    LogUtil.Error(ex);
                }
            }
        }

        private static void Send(string socketClientId, string content)
        {
            MsgContent msgContent = new MsgContent();
            msgContent.Content = content;

            _socketServerHelper.Send(msgContent, socketClientId, (r) =>
            {
                if (r.Success)
                {
                    LogUtil.Debug("收到客户端 " + socketClientId + " 成功反馈");
                }
                else
                {
                    LogUtil.Debug("收到客户端 " + socketClientId + " 失败反馈，失败消息：" + r.Msg);
                }
            });

            LogUtil.Debug("向客户端 " + socketClientId + " 发送消息");
        }
    }
}
