using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace BoltServer.WebSockets
{
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

        public string AddSocket(WebSocket socket)
        {
            var connectionId = Guid.NewGuid().ToString();

            _sockets.TryAdd(connectionId, socket);

            return connectionId;
        }

        public ConcurrentDictionary<string, WebSocket> GetAllSockets()
        {
            return _sockets;
        }

        public WebSocket? GetSocketById(string id)
        {
            _sockets.TryGetValue(id, out var socket);
            return socket;
        }

        public string GetId(WebSocket socket)
        {
            return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }

        public async Task RemoveSocket(string id)
        {
            if (_sockets.TryRemove(id, out var socket))
            {
                if (socket.State != WebSocketState.Closed &&
                    socket.State != WebSocketState.Aborted)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed by the server",
                        CancellationToken.None);
                }
            }
        }

        public int GetConnectedCount()
        {
            return _sockets.Count(pair => pair.Value.State == WebSocketState.Open);
        }

    }
}
