using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoltClient
{
    public enum WebSocketConnectionState
    {
        Initial,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected,
        Error
    }

    public class WebSocketClient : IDisposable
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private WebSocketConnectionState _state;
        private readonly string _serverUrl;
        private readonly object _stateLock = new object();

        public event EventHandler<WebSocketConnectionState> ConnectionStateChanged;
        public event EventHandler<string> MessageReceived;

        public WebSocketConnectionState State
        {
            get => _state;
            private set
            {
                lock (_stateLock)
                {
                    if (_state != value)
                    {
                        _state = value;
                        ConnectionStateChanged?.Invoke(this, _state);
                    }
                }
            }
        }

        public WebSocketClient(string serverUrl)
        {
            _serverUrl = serverUrl;
            _state = WebSocketConnectionState.Initial;
        }

        public async Task ConnectAsync()
        {
            if (State == WebSocketConnectionState.Connected)
                return;

            State = WebSocketConnectionState.Connecting;

            try
            {
                _webSocket = new ClientWebSocket();
                _cts = new CancellationTokenSource();

                await _webSocket.ConnectAsync(new Uri(_serverUrl), _cts.Token);

                State = WebSocketConnectionState.Connected;

                // Start receiving messages
                _ = ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
                State = WebSocketConnectionState.Error;
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (State != WebSocketConnectionState.Connected)
                return;

            State = WebSocketConnectionState.Disconnecting;

            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting",
                    _cts?.Token ?? CancellationToken.None);
                _cts?.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disconnect error: {ex.Message}");
            }
            finally
            {
                State = WebSocketConnectionState.Disconnected;
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (State != WebSocketConnectionState.Connected)
                throw new InvalidOperationException("WebSocket is not connected");

            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text,
                true, _cts.Token);
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];

            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge close frame",
                            _cts.Token);
                        State = WebSocketConnectionState.Disconnected;
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal during disconnection
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                State = WebSocketConnectionState.Error;
            }
            finally
            {
                if (State != WebSocketConnectionState.Disconnected &&
                    State != WebSocketConnectionState.Disconnecting)
                {
                    State = WebSocketConnectionState.Disconnected;
                }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _webSocket?.Dispose();
        }
    }
}
