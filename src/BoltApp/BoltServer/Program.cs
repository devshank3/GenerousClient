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

            app.MapGet("/", () => "Bolt Server");

            app.Run();
        }
    }
}
