using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SocketUtil
{
    /// <summary>
    /// RPC返回结果
    /// </summary>
    [Serializable]
    public class RpcResult
    {
        /// <summary>
        /// 方法返回值
        /// </summary>
        public object returnValue { get; set; }

        /// <summary>
        /// 方法参数
        /// </summary>
        public ParameterInfo[] param { get; set; }

        /// <summary>
        /// 方法参数值
        /// </summary>
        public object[] paramValue { get; set; }
    }
}
