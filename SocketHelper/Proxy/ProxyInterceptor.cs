using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 拦截器
    /// </summary>
    public class ProxyInterceptor : IInterceptor
    {
        private string _serviceName;

        private string _socketClientId;

        public ProxyInterceptor(string serviceName, string socketClientId)
        {
            _serviceName = serviceName;
            _socketClientId = socketClientId;
        }

        /// <summary>
        /// 拦截方法
        /// </summary>
        public void Intercept(IInvocation invocation)
        {
            //准备参数
            ParameterInfo[] parameterInfoArr = invocation.Method.GetParameters();
            object[] valArr = new object[parameterInfoArr.Length];
            for (int i = 0; i < parameterInfoArr.Length; i++)
            {
                valArr[i] = invocation.GetArgumentValue(i);
            }

            //执行方法
            try
            {
                RequestUtil.DoRequest(_socketClientId, _serviceName, invocation.Method.Name, parameterInfoArr, valArr);
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex, "ProxyInterceptor " + _serviceName + " " + invocation.Method.Name + " 异常");
            }
        }
    }
}
