using BoltServer.Hubs;

namespace BoltServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR();

            var app = builder.Build();

            app.MapHub<AccessorHub>("/accessorHub");

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            };

            app.UseWebSockets(webSocketOptions);

            app.MapGet("/", () => "Bolt Server");


            app.Run();


        }
    }
}
