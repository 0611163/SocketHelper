using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 操作结果事件参数
    /// </summary>
    public class SocketResultEventArgs : EventArgs
    {
        private SocketResult _SocketResult;
        /// <summary>
        /// 数据
        /// </summary>
        public SocketResult SocketResult
        {
            get { return _SocketResult; }
            set { _SocketResult = value; }
        }

        private ClientSocket _ClientSocket;
        /// <summary>
        /// 客户端Socket对象
        /// </summary>
        public ClientSocket ClientSocket
        {
            get { return _ClientSocket; }
            set { _ClientSocket = value; }
        }

        public SocketResultEventArgs(ClientSocket clientSocket, SocketResult socketResult)
        {
            _ClientSocket = clientSocket;
            _SocketResult = socketResult;
        }
    }
}
