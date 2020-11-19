using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// Socket封装
    /// </summary>
    public static class SocketHelper
    {
        #region 变量

        #endregion

        #region Send
        /// <summary>
        /// Send
        /// </summary>
        public static bool Send(Socket socket, byte[] data)
        {
            try
            {
                if (socket == null || !socket.Connected) return false;

                int sendTotal = 0;
                while (sendTotal < data.Length)
                {
                    int sendLength = data.Length - sendTotal;
                    if (sendLength > 1024) sendLength = 1024;
                    int sendOnce = socket.Send(data, sendTotal, sendLength, SocketFlags.None);
                    sendTotal += sendOnce;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex);
                return false;
            }
        }
        #endregion

    }

}
