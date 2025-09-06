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

            // Add controllers and Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //WebSocket services
            builder.Services.AddSingleton<WebSocketConnectionManager>();
            builder.Services.AddSingleton<WebSocketHandler, BoltWebSocketHandler>();

            var app = builder.Build();

            // Configure Swagger
            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapHub<AccessorHub>("/accessorHub");
            app.MapControllers();

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
