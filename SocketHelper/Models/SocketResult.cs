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
        public bool Success { get; set; }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 回调ID
        /// </summary>
        public string CallbackId { get; set; }

        /// <summary>
        /// 收到返回值时间
        /// </summary>
        public DateTime CallbackTime { get; set; }
    }
}
