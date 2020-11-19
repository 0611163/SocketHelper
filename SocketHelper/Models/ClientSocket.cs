using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 客户端Socket对象
    /// </summary>
    public class ClientSocket
    {
        /// <summary>
        /// Socket客户端ID
        /// </summary>
        public string SocketClientId { get; set; }

        /// <summary>
        /// Socket对象
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// 异步参数
        /// </summary>
        public SocketAsyncEventArgs SocketAsyncArgs { get; set; }

        /// <summary>
        /// 异步接收方法
        /// </summary>
        public EventHandler<SocketAsyncEventArgs> SocketAsyncCompleted { get; set; }

        /// <summary>
        /// 客户端操作结果回调
        /// </summary>
        public ConcurrentDictionary<string, SocketResult> CallbackDict = new ConcurrentDictionary<string, SocketResult>();

        /// <summary>
        /// 上次心跳时间
        /// </summary>
        public DateTime LastHeartbeat { get; set; }

        /// <summary>
        /// 缓冲区
        /// </summary>
        public List<byte> Buffer { get; set; }

        /// <summary>
        /// 锁
        /// </summary>
        public object LockSend { get; set; }

        /// <summary>
        /// 客户端Socket对象
        /// </summary>
        public ClientSocket(Socket socket)
        {
            Socket = socket;
            Buffer = new List<byte>();
            LastHeartbeat = DateTime.Now;
            LockSend = new object();
        }

        /// <summary>
        /// 删除接收到的一个包
        /// </summary>
        public void RemoveBufferData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (Buffer.Count > 0)
                {
                    Buffer.RemoveAt(0);
                }
            }
        }
    }
}
