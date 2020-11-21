using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketUtil
{
    /// <summary>
    /// 用于回调的Socket封装
    /// </summary>
    public class CallbackSocket
    {
        protected Socket _socket;
        protected ClientSocket _clientSocket;

        public CallbackSocket(Socket socket)
        {
            _socket = socket;
        }

        public CallbackSocket(ClientSocket clientSocket)
        {
            _clientSocket = clientSocket;
        }

        public void SendResult(SocketClientHelper socketClientHelper, SocketResult socketResult, SocketReceivedEventArgs e)
        {
            SocketData data = new SocketData();
            data.Type = SocketDataType.返回值;
            data.SocketResult = socketResult;
            socketResult.CallbackId = e.Content.CallbackId;
            socketClientHelper.Send(_socket, data);
        }

        public void SendResult(SocketServerHelper socketServerHelper, SocketResult socketResult, SocketReceivedEventArgs e)
        {
            SocketData data = new SocketData();
            data.Type = SocketDataType.返回值;
            data.SocketResult = socketResult;
            socketResult.CallbackId = e.Content.CallbackId;
            socketServerHelper.Send(_clientSocket, data);
        }
    }
}
