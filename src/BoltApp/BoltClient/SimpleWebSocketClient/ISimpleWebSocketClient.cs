using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using BoltClient.SimpleWebSocketClient.Models;

namespace BoltClient.SimpleWebSocketClient
{
    /// <summary>
    /// A simplified websocket client interface with built-in reconnection capabilities
    /// </summary>
    public interface ISimpleWebsocketClient : IDisposable
    {

        /// <summary>
        /// Stream for notification messages
        /// </summary>
        IObservable<ResponseMessage> NotificationReceived { get; }

        /// <summary>
        /// Stream for disconnection events
        /// </summary>
        IObservable<ConnectionInfo> ConnectionStateChanged { get; }

        /// <summary>
        /// Start the websocket connection
        /// </summary>
        Task Start();

        /// <summary>
        /// Stop the websocket connection
        /// </summary>
        Task Stop();

        Task Send(string message);

        Task Send(byte[] message);

        bool IsReconnectionEnabled { get; set; }

    }
}