using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// Socket消息数据内容
    /// </summary>
    public class MsgContent
    {
        /// <summary>
        /// 回调ID(GUID)
        /// </summary>
        public string CallbackId { get; set; }

        /// <summary>
        /// 消息数据
        /// </summary>
        public string Content { get; set; }

        public MsgContent()
        {
            CallbackId = Guid.NewGuid().ToString("N");
        }
    }
}
