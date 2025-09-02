using BoltServer.Hubs;
using BoltServer.WebSockets;

namespace BoltServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR();

            //WebSocket services
            builder.Services.AddSingleton<WebSockets.WebSocketConnectionManager>();
            builder.Services.AddSingleton<WebSocketHandler, BoltWebSocketHandler>();

            var app = builder.Build();

            app.MapHub<AccessorHub>("/accessorHub");

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            };

            app.UseWebSockets(webSocketOptions);
            app.UseWebSocketMiddleware("/ws");

            app.MapGet("/", () => "Bolt Server");

            app.MapGet("/status", (WebSocketConnectionManager manager) =>
                $"Connected clients: {manager.GetConnectedCount()}");


            app.Run();


        }
    }
}
