using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace BoltClient.SimpleWebSocketClient.Models
{
    /// <summary>
    /// Represents a message received from the WebSocket server
    /// </summary>
    public class ResponseMessage
    {
        private readonly byte[] _binary;

        private ResponseMessage(byte[] binary, string text, WebSocketMessageType messageType)
        {
            _binary = binary;
            Text = text;
            MessageType = messageType;
        }

        /// <summary>
        /// Text content if message type is Text
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Binary content if message type is Binary
        /// </summary>
        public byte[] Binary => _binary;

        /// <summary>
        /// Type of received message
        /// </summary>
        public WebSocketMessageType MessageType { get; }

        /// <summary>
        /// Create a text message
        /// </summary>
        public static ResponseMessage TextMessage(string data)
        {
            return new ResponseMessage(null, data, WebSocketMessageType.Text);
        }

        /// <summary>
        /// Create a binary message
        /// </summary>
        public static ResponseMessage BinaryMessage(byte[] data)
        {
            return new ResponseMessage(data, null, WebSocketMessageType.Binary);
        }

        /// <summary>
        /// String representation of the message
        /// </summary>
        public override string ToString()
        {
            if (MessageType == WebSocketMessageType.Text)
            {
                return Text ?? string.Empty;
            }

            return $"Binary message, length: {Binary?.Length ?? 0}";
        }
    }
}
