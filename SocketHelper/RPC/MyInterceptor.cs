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
    public class MyInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            //准备参数
            ParameterInfo[] parameterInfoArr = invocation.Method.GetParameters();
            object[] valArr = new object[parameterInfoArr.Length];
            for (int i = 0; i < parameterInfoArr.Length; i++)
            {
                valArr[i] = invocation.GetArgumentValue(i);
            }

            RpcResult result = RequestUtil.DoRequest(invocation.TargetType.FullName, invocation.Method.Name, parameterInfoArr, valArr);

            //返回结果和out参数
            invocation.ReturnValue = result.returnValue;
            for (int i = 0; i < parameterInfoArr.Length; i++)
            {
                ParameterInfo paramInfo = parameterInfoArr[i];
                if (paramInfo.IsOut)
                {
                    invocation.SetArgumentValue(i, result.paramValue[i]);
                }
            }
        }
    }
}
