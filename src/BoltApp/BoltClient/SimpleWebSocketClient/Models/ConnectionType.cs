using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoltClient.SimpleWebSocketClient.Models
{
    public enum ConnectionState
    {
        Initial = 0,
        Lost = 1,
        Error = 2,
        Connected = 3,
        Disconnected = 4,
        Reconnecting = 5
    }
}
