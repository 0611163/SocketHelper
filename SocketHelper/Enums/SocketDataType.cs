using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// Socket数据类型
    /// </summary>
    public enum SocketDataType
    {
        心跳 = 1,
        心跳应答 = 2,
        注册 = 3,
        注册反馈 = 4,
        消息数据 = 5,
        返回值 = 6
    }
}
