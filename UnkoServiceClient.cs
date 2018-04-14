using System;

#if NETFX_CORE
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

namespace UnkoService
{
    public class UnkoServiceClient
    {
        public delegate void MessageReceivedEventHandler(object sender, string message);
        public event MessageReceivedEventHandler OnMessageReceived;

#if NETFX_CORE
        private MessageWebSocket WebSocket { get; set; }

        public void Initialize()
        {
            this.WebSocket = new MessageWebSocket();
            this.WebSocket.Control.MessageType = SocketMessageType.Utf8;
            this.WebSocket.MessageReceived += this.MessageReceived;
            this.WebSocket.Closed += this.Closed;
        }

        public void Connect(Uri uri, string connectedMessage)
        {
            Task.Run(async () => {
                await Task.Run(async () =>
                {
                    await this.WebSocket.ConnectAsync(uri);
                    await this.SendMessage(connectedMessage);
                });
            });
        }

        public async Task SendMessage(string message)
        {
            await this.SendMessage(this.WebSocket, message);
        }

        private async Task SendMessage(MessageWebSocket webSocket, string message)
        {
            var messageWriter = new DataWriter(webSocket.OutputStream);
            messageWriter.WriteString(message);
            await messageWriter.StoreAsync();
        }

        private void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            var messageReader = args.GetDataReader();
            messageReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            var messageString = messageReader.ReadString(messageReader.UnconsumedBufferLength);
            this.OnMessageReceived?.Invoke(this, messageString);
        }

        private void Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
        }
#endif
    }
}
