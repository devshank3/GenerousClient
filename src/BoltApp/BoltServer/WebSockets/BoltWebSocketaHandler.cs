using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace BoltServer.WebSockets
{
    public class BoltWebSocketHandler : WebSocketHandler
    {
        public BoltWebSocketHandler(WebSocketConnectionManager connectionManager)
            : base(connectionManager)
        {
        }

        public override async Task OnConnected(WebSocket socket, string connectionId)
        {
            // Send welcome message to the connected client
            await SendMessageAsync(connectionId, $"Welcome! Your connection ID is: {connectionId}");

            // Broadcast connection notification
            await SendMessageToAllAsync($"Client {connectionId} has joined");
        }

        public override async Task OnDisconnected(string connectionId)
        {
            // Broadcast disconnection notification
            await SendMessageToAllAsync($"Client {connectionId} has left");
        }

        public override async Task ReceiveAsync(string connectionId, string message)
        {
            // Process the message
            // This is where you would implement your specific message handling logic

            // Echo the message back to the sender
            await SendMessageAsync(connectionId, $"Server received: {message}");
        }
    }
}