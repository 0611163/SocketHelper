using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// Socket数据
    /// </summary>
    [Serializable]
    public class SocketData
    {
        /// <summary>
        /// Socket包头
        /// </summary>
        public static string HeaderString = "0XFF";

        /// <summary>
        /// 类型 1心跳 2心跳应答 3注册包 4注册反馈 5消息数据 6返回值
        /// </summary>
        public SocketDataType Type { get; set; }

        /// <summary>
        /// 消息数据
        /// </summary>
        public MsgContent Content { get; set; }

        /// <summary>
        /// 操作结果
        /// </summary>
        public SocketResult SocketResult { get; set; }

        /// <summary>
        /// 注册包数据
        /// </summary>
        public SocketRegisterData SocketRegisterData { get; set; }
    }
}
