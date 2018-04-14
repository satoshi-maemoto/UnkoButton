using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UnkoService
{
    public class WSClient : IObservable<Message>, IObserver<Message>, IDisposable
    {
        private readonly Subject<Message> receiveSubject;
        private readonly WebSocket socket;

        public WSClient(WebSocket socket)
        {
            this.socket = socket;
            this.receiveSubject = new Subject<Message>();
        }

        public bool IsOpen => this.socket.State == WebSocketState.Open;

        public bool IsJoin { get; private set; }

        public string ClientName { get; private set; } = string.Empty;

        public string Id { get; private set; } = Guid.NewGuid().ToString();

        public void Dispose()
        {
            this.receiveSubject?.Dispose();
            this.socket?.Dispose();
        }

        public IDisposable Subscribe(IObserver<Message> observer)
        {
            return this.receiveSubject.Subscribe(observer);
        }

        public void OnCompleted()
        {
        }

        public async void OnError(Exception error)
        {
            if (this.socket.State == WebSocketState.Open)
            {
                await this.socket.CloseOutputAsync(WebSocketCloseStatus.InternalServerError, error.Message, CancellationToken.None);
            }
        }

        public async void OnNext(Message value)
        {
            if (this.socket.State == WebSocketState.Open)
            {
                await this.socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value))), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async Task RecieveJoinAsync(int timeout = 5000)
        {
            var buffer = new byte[4096];
            var tokensource = new CancellationTokenSource();
            tokensource.CancelAfter(timeout);
            var result = await this.socket.ReceiveAsync(new ArraySegment<byte>(buffer), tokensource.Token);

            if (result.MessageType == WebSocketMessageType.Text && result.EndOfMessage)
            {
                var joinmessage = JsonConvert.DeserializeObject<JoinMessage>(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (
                    JoinMessage.MessageTypeKeyword.Equals(joinmessage.MessageType,
                        StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrWhiteSpace(joinmessage.ClientName))
                {
                    this.IsJoin = true;
                    this.ClientName = joinmessage.ClientName;
                }
            }
        }

        public async Task ReceiveAsync()
        {
            if (!this.IsJoin)
            {
                return;
            }

            var resultCount = 0;
            var buffer = new byte[4096];
            while (true)
            {
                var segmentbuffer = new ArraySegment<byte>(buffer, resultCount, buffer.Length - resultCount);
                var result = await this.socket.ReceiveAsync(segmentbuffer, CancellationToken.None);
                resultCount += result.Count;
                if (resultCount >= buffer.Length)
                {
                    Debug.WriteLine("Long Message!!!");
                    await this.socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "Long Message", CancellationToken.None);
                    this.socket.Dispose();
                    this.receiveSubject.OnCompleted();
                }
                else if (result.EndOfMessage)
                {
                    if (result.MessageType == WebSocketMessageType.Close ||　resultCount == 0)
                    {
                        this.receiveSubject.OnCompleted();
                        break;
                    }
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = JsonConvert.DeserializeObject<Message>(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        this.receiveSubject.OnNext(new Message
                        {
                            ClientName = this.ClientName,
                            MessageType = message.MessageType,
                            What = message.What,
                            RecieveTime = DateTimeOffset.Now
                        });
                        resultCount = 0;
                    }
                    else
                    {
                        this.receiveSubject.OnCompleted();
                        break;
                    }
                }
            }
        }
    }
}