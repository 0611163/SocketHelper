using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// RPC数据
    /// </summary>
    [Serializable]
    public class RpcData
    {
        /// <summary>
        /// 接口名称
        /// </summary>
        public string interfaceName { get; set; }

        /// <summary>
        /// 方法名
        /// </summary>
        public string methodName { get; set; }

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
