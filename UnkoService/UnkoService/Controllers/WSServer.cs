using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace UnkoService
{
    public class WSServer
    {
        private readonly AsyncLock asyncLock = new AsyncLock();
        private readonly List<WSClient> clients = new List<WSClient>();

        public void Map(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.Use(this.Acceptor);
        }

        public async void BroadcastMessage(Message message)
        {
            using (await this.asyncLock.LockAsync())
            {
                this.clients.AsParallel().ForAll(
                    x =>
                    {
                        // 入室メッセージを送信
                        x.OnNext(message);
                    });
            }
        }

        private async Task Acceptor(HttpContext hc, Func<Task> n)
        {
            if (!hc.WebSockets.IsWebSocketRequest)
            {
                await n.Invoke();
                return;
            }
            var websocket = await hc.WebSockets.AcceptWebSocketAsync();
            var client = new WSClient(websocket);

            await client.RecieveJoinAsync();

            if (!client.IsJoin)
            {
                return;
            }
            using (await this.asyncLock.LockAsync())
            {
                this.clients.AsParallel().ForAll(
                    x =>
                    {
                        // 入室メッセージを送信
                        x.OnNext(new Message
                        {
                            ClientName = "Service",
                            What = $"{client.ClientName} is conncected.",
                            RecieveTime = DateTimeOffset.Now
                        });

                        // ほかのクライアントと相互接続
                        x.Subscribe(client);
                        client.Subscribe(x);
                    });

                // エコーバック
                client.Subscribe(client);

                // 切断時動作
                client.Subscribe(s => { }, async () => await Close(client));

                // クライアント登録
                this.clients.Add(client);
            }

            // 受信待機
            await client.ReceiveAsync();
        }

        private async Task Close(WSClient client)
        {
            using (await this.asyncLock.LockAsync())
            {
                this.clients.Remove(client);

                this.clients.ForEach(
                    x =>
                    {
                        this.clients.ForEach(
                            y =>
                                y.OnNext(new Message
                                {
                                    ClientName = "Service",
                                    What = $"{x.ClientName} is disconnected.",
                                    RecieveTime = DateTimeOffset.Now
                                }));
                    });
            }
        }
    }
}