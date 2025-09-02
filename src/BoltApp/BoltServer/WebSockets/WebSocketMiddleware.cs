using System.Net.WebSockets;
using System.Text;

namespace BoltServer.WebSockets
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketConnectionManager _manager;
        private readonly WebSocketHandler _handler;

        public WebSocketMiddleware(RequestDelegate next, WebSocketConnectionManager webSocketManager, WebSocketHandler handler)
        {
            _next = next;
            _manager = webSocketManager;
            _handler = handler;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var connectionId = _manager.AddSocket(socket);
            try
            {
                await _handler.OnConnected(socket, connectionId);

                await ProcessWebSocket(socket, connectionId);

            }
            catch (WebSocketException ex)
            {
                
            }
            finally
            {
                await _manager.RemoveSocket(connectionId);
                await _handler.OnDisconnected(connectionId);
            }
        }

        private async Task ProcessWebSocket(WebSocket webSocket, string connectionId)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await _handler.ReceiveAsync(connectionId, message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed by the client",
                        CancellationToken.None);
                    break;
                }
            }
        }
    }
}
