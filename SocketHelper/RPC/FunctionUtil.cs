using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 执行方法
    /// </summary>
    public class FunctionUtil
    {
        private static ConcurrentDictionary<Type, object> _objs = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// 执行方法
        /// </summary>
        public static object RunFunction(Assembly assembly, string nameSpace, RpcData socketData)
        {
            Type type = assembly.GetType(nameSpace + "." + socketData.interfaceName);
            MethodInfo methodInfo = type.GetMethod(socketData.methodName);
            ParameterInfo[] parameterInfoArr = methodInfo.GetParameters();
            for (int i = 0; i < parameterInfoArr.Length; i++)
            {
                ParameterInfo paramInfo = parameterInfoArr[i];
                HandleParam(paramInfo, ref socketData, i);
            }
            var instance = _objs.GetOrAdd(type, t => Activator.CreateInstance(t));
            object result = methodInfo.Invoke(instance, socketData.paramValue);
            RpcResult rpcResult = new RpcResult();
            rpcResult.returnValue = result;
            rpcResult.paramValue = new object[socketData.param.Length];
            object paramObj;
            for (int i = 0; i < parameterInfoArr.Length; i++)
            {
                paramObj = socketData.paramValue[i];
                if (parameterInfoArr[i].ParameterType.IsByRef || parameterInfoArr[i].IsOut)
                {
                    rpcResult.paramValue[i] = paramObj;
                }
                else
                {
                    rpcResult.paramValue[i] = null;
                }
            }
            return result;
        }

        private static void HandleParam(ParameterInfo paramInfo, ref RpcData socketData, int i)
        {
            if (paramInfo.ParameterType == typeof(int))
            {
                socketData.paramValue[i] = Convert.ToInt32(socketData.paramValue[i]);
            }
            if (paramInfo.ParameterType == typeof(long))
            {
                socketData.paramValue[i] = Convert.ToInt64(socketData.paramValue[i]);
            }
            if (paramInfo.ParameterType == typeof(float))
            {
                socketData.paramValue[i] = (float)socketData.paramValue[i];
            }
            if (paramInfo.ParameterType == typeof(double))
            {
                socketData.paramValue[i] = Convert.ToDouble(socketData.paramValue[i]);
            }
            if (paramInfo.ParameterType == typeof(decimal))
            {
                socketData.paramValue[i] = Convert.ToDecimal(socketData.paramValue[i]);
            }
        }
    }
}
