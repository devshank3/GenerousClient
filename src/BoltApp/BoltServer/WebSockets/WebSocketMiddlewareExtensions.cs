namespace BoltServer.WebSockets
{
    public static class WebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSocketMiddleware(this IApplicationBuilder builder, string path)
        {
            //return builder.UseMiddleware<WebSocketMiddleware>();

            return builder.Map(path, (appBuilder) =>
            {
                appBuilder.UseWebSockets();
                appBuilder.UseMiddleware<WebSocketMiddleware>();
            });
        }
    }
}
