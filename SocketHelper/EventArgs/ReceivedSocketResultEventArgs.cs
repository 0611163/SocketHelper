using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocketUtil
{
    /// <summary>
    /// 收到返回值包事件参数
    /// </summary>
    public class ReceivedSocketResultEventArgs : EventArgs
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

        public ReceivedSocketResultEventArgs(SocketResult socketResult)
        {
            _SocketResult = socketResult;
        }

    }
}