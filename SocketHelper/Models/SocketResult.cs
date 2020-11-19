using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketUtil
{
    /// <summary>
    /// Socket返回
    /// </summary>
    [Serializable]
    public class SocketResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string errorMsg { get; set; }

        /// <summary>
        /// 回调ID
        /// </summary>
        public string callbackId { get; set; }
    }
}
