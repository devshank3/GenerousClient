using System.Net.WebSockets;
using System.Text;

namespace BoltServer.WebSockets
{
    public abstract class WebSocketHandler
    {
        protected readonly WebSocketConnectionManager ConnectionManager;

        protected WebSocketHandler(WebSocketConnectionManager connectionManager)
        {
            ConnectionManager = connectionManager;
        }

        public virtual async Task OnConnected(WebSocket socket, string connectionId)
        {
            await Task.CompletedTask;
        }

        public virtual async Task OnDisconnected(string connectionId)
        {
            await Task.CompletedTask;
        }

        public virtual async Task ReceiveAsync(string connectionId, string message)
        {
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(string connectionId, string message)
        {
            var socket = ConnectionManager.GetSocketById(connectionId);

            if (socket == null || socket.State != WebSocketState.Open)
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(message);

            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        public async Task SendMessageToAllAsync(string message)
        {
            foreach (var pair in ConnectionManager.GetAllSockets())
            {
                if (pair.Value.State == WebSocketState.Open)
                {
                    await SendMessageAsync(pair.Key, message);
                }
            }
        }
    }
}