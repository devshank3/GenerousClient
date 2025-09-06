using Microsoft.AspNetCore.Connections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BoltClient.SimpleWebSocketClient.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace BoltClient.SimpleWebSocketClient
{
    public class SimpleWebSocketClient : ISimpleWebsocketClient
    {
        private readonly Uri _url;
        private readonly Func<Uri, CancellationToken, Task<WebSocket>> _connectionFactory;

        private WebSocket? _client;
        private CancellationTokenSource? _cancellation;
        private bool _disposed;
        private bool _reconnecting;
        private bool _isStarted;
        private bool _isRunning;

        private readonly Subject<ResponseMessage> _notificationSubject = new Subject<ResponseMessage>();
        private readonly Subject<ConnectionInfo> _connectionStateSubject = new Subject<ConnectionInfo>();

        public SimpleWebSocketClient(Uri url)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
            _connectionFactory =  CreateDefaultWebSocket;
        }

        /// <inheritdoc />
        public IObservable<ResponseMessage> NotificationReceived => _notificationSubject.AsObservable();

        /// <inheritdoc />
        public IObservable<ConnectionInfo> ConnectionStateChanged => _connectionStateSubject.AsObservable();


        /// <summary>
        /// Gets or sets whether automatic reconnection is enabled
        /// </summary>
        public bool IsReconnectionEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the delay between reconnection attempts
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <inheritdoc />
        public async Task Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SimpleWebSocketClient));
            }

            if (_isStarted)
            {
                return;
            }

            _isStarted = true;
            _cancellation = new CancellationTokenSource();

            await ConnectWithRetry(_cancellation.Token);
        }

        /// <inheritdoc />
        public async Task Stop()
        {
            if (!_isStarted)
            {
                return;
            }

            try
            {
                if (_client != null && _isRunning)
                {
                    await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing",
                        _cancellation?.Token ?? CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                _isRunning = false;
                _isStarted = false;
                _cancellation?.Cancel();
                _connectionStateSubject.OnNext(new ConnectionInfo(ConnectionState.Disconnected, WebSocketCloseStatus.NormalClosure));
            }
        }

        /// <summary>
        /// Sends a text message over the WebSocket connection
        /// </summary>
        public async Task Send(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            if (!_isRunning || _client == null)
                throw new InvalidOperationException("WebSocket client is not connected");

            var bytes = Encoding.UTF8.GetBytes(message);
            await _client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text,
                true, _cancellation?.Token ?? CancellationToken.None);
        }

        /// <summary>
        /// Sends a binary message over the WebSocket connection
        /// </summary>
        public async Task Send(byte[] message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (!_isRunning || _client == null)
                throw new InvalidOperationException("WebSocket client is not connected");

            await _client.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Binary,
                true, _cancellation?.Token ?? CancellationToken.None);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _cancellation?.Cancel();
                _client?.Abort();
                _client?.Dispose();
                _cancellation?.Dispose();
                _notificationSubject.OnCompleted();
                _connectionStateSubject.OnCompleted();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                _isRunning = false;
                _isStarted = false;
            }
        }

        private async Task<WebSocket> CreateDefaultWebSocket(Uri uri, CancellationToken token)
        {
            var client = new ClientWebSocket();
            await client.ConnectAsync(uri, token);
            return client;
        }

        private async Task ConnectWithRetry(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _connectionStateSubject.OnNext(new ConnectionInfo(ConnectionState.Reconnecting, null));
                    _client = await _connectionFactory(_url, cancellationToken);
                    _isRunning = true;
                    _connectionStateSubject.OnNext(new ConnectionInfo(ConnectionState.Connected, null));

                    await Listen(cancellationToken);

                    // If we reached here normally, the connection was closed gracefully
                    if (!_reconnecting)
                        return;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _isRunning = false;
                    _connectionStateSubject.OnNext(new ConnectionInfo(ConnectionState.Error, null));

                    if (!IsReconnectionEnabled)
                    {
                        _connectionStateSubject.OnNext(new ConnectionInfo(ConnectionState.Disconnected, null));
                        _isStarted = false;
                        throw;
                    }

                    await Task.Delay(ReconnectDelay, cancellationToken);
                }
            }
        }

        private async Task Listen(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192]; // 8KB buffer
            var receiveBuffer = new ArraySegment<byte>(buffer);

            try
            {
                while (_client!.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _client.ReceiveAsync(receiveBuffer, cancellationToken);
                        if (result.Count > 0)
                            ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage && !cancellationToken.IsCancellationRequested);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                        _isRunning = false;
                        _connectionStateSubject.OnNext(new ConnectionInfo(ConnectionState.Disconnected, result.CloseStatus));
                        return;
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    ResponseMessage message;
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using var reader = new StreamReader(ms, Encoding.UTF8);
                        var text = await reader.ReadToEndAsync();
                        message = ResponseMessage.TextMessage(text);
                    }
                    else
                    {
                        var binary = ms.ToArray();
                        message = ResponseMessage.BinaryMessage(binary);
                    }

                    _notificationSubject.OnNext(message);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("WebSocket connection lost");
                _isRunning = false;
                _connectionStateSubject.OnNext(new ConnectionInfo(ConnectionState.Lost, _client?.CloseStatus));

                if (IsReconnectionEnabled && !_disposed && _isStarted)
                {
                    _reconnecting = true;

                    // Clean up existing connection
                    try
                    {
                        _client?.Abort();
                        _client?.Dispose();
                        _client = null;
                    }
                    catch (Exception disposeEx)
                    {
                        Console.WriteLine("Error while cleaning up WebSocket connection");
                    }

                    // Reconnection will happen in the outer ConnectWithRetry loop
                    await Task.Delay(ReconnectDelay, cancellationToken);
                }
                else
                {
                    _isStarted = false;
                }
            }
        }

    }
    
}
