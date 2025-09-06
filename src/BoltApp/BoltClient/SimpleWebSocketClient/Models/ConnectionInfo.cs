using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace BoltClient.SimpleWebSocketClient.Models
{
    public class ConnectionInfo
    {
        public ConnectionInfo(ConnectionState state, WebSocketCloseStatus? closeStatus)
        {
            State = state;
            CloseStatus = closeStatus;
        }

        public ConnectionState State { get; }

        public WebSocketCloseStatus? CloseStatus { get;  }

        public static ConnectionInfo Create(ConnectionState state, WebSocket? client)
        {
            return new ConnectionInfo(state, client?.CloseStatus);
        }
    }
}
