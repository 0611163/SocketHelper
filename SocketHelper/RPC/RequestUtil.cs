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
        /// <param name="interfaceName">接口名称</param>
        /// <param name="methodName">实现方法名称</param>
        /// <param name="args">实现方法的参数集合</param>
        public static void DoRequest(string interfaceName, string methodName, ParameterInfo[] parameterInfoArr, object[] args)
        {
            //准备POST数据
            RpcData postData = new RpcData();
            postData.interfaceName = interfaceName;
            postData.methodName = methodName;
            postData.param = parameterInfoArr;
            postData.paramValue = args;
            string strJson = JsonConvert.SerializeObject(postData);

            //网络请求
            foreach (string socketClientId in _socketServerHelper.GetSocketClientIdListAll())
            {
                try
                {
                    for (int i = 0; i < 1; i++)
                    {
                        MsgContent content = new MsgContent();
                        content.Content = strJson;

                        _socketServerHelper.Send(content, socketClientId, (r) =>
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
                catch (Exception ex)
                {
                    LogUtil.Error(ex);
                }
            }
        }
    }
}
