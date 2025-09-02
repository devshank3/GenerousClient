using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace BoltClient
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            Console.WriteLine("Connecting to WebSocket server...");
            var wsClient = new WebSocketClient("wss://localhost:7063/ws");
            wsClient.ConnectionStateChanged += (sender, state) =>
                Console.WriteLine($"Connection state changed to: {state}");
            wsClient.MessageReceived += (sender, message) =>
                Console.WriteLine($"Message received: {message}");

            await wsClient.ConnectAsync();

            Console.WriteLine("Connected! Type a message to send or 'exit' to quit");

            string input;
            while ((input = Console.ReadLine()) != "exit")
            {
                if (!string.IsNullOrEmpty(input))
                {
                    await wsClient.SendMessageAsync(input);
                }
            }
        }

        //static async Task Main(string[] args)
        //{
        //    var connection = new HubConnectionBuilder()
        //        .WithUrl("https://localhost:7063/accessorHub")
        //        .Build();


        //    connection.Closed += async (error) =>
        //    {
        //        Console.WriteLine("Connection closed. Trying to restart...");
        //        await Task.Delay(new Random().Next(0, 5) * 1000);
        //        await connection.StartAsync();
        //    };

        //    connection.Reconnecting += error =>
        //    {
        //        Console.WriteLine("Connection lost. Reconnecting...");
        //        return Task.CompletedTask;
        //    };

        //    connection.Reconnected += connectionId =>
        //    {
        //        Console.WriteLine($"Reconnected. ConnectionId: {connectionId}");
        //        return Task.CompletedTask;
        //    };

        //    connection.On<int>("UpdateViewCount", (viewCount) =>
        //    {
        //        Console.WriteLine($"View count: {viewCount}");
        //    });

        //    try
        //    {
        //        connection.StartAsync().Wait();
        //        Console.WriteLine("Connection started successfully.");
        //        await connection.InvokeAsync("NotifyWatching");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Connection failed: {ex.Message}");
        //    }

        //    Console.ReadLine();


        //}
    }
}
