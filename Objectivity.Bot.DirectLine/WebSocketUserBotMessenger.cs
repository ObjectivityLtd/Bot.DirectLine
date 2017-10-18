namespace Objectivity.Bot.DirectLine
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.DirectLine;
    using Newtonsoft.Json;
    using NLog;
    using Objectivity.Bot.DirectLine.DirectLine;

    public class WebSocketUserBotMessenger : BaseUserBotMessenger, IDisposable
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private bool disposed = false;

        private CancellationTokenSource webSocketCancellationTokenSource = new CancellationTokenSource();

        private ClientWebSocket socket = new ClientWebSocket();

        public WebSocketUserBotMessenger(IDirectLineConversationService directLineConversationService)
            : base(directLineConversationService)
        {
        }

        public override void StopListening()
        {
            this.IsListening = false;
            this.webSocketCancellationTokenSource.Cancel();
        }

        public override async Task StartListening(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.IsListening)
            {
                return;
            }

            this.IsListening = true;

            try
            {
                this.MessengerInitialized.Set();
                WebSocketUserBotMessenger.Logger.Debug($"{this.UserId} MessengerInitialized.Set() called.");

                while (this.IsListening)
                {
                    try
                    {
                        var jsonMessageReceived = await this.ReadMessageFromSocket();

                        if (!string.IsNullOrEmpty(jsonMessageReceived))
                        {
                            var activitySet = JsonConvert.DeserializeObject<ActivitySet>(jsonMessageReceived);

                            if (!string.IsNullOrEmpty(activitySet.Watermark))
                            {
                                await
                                    this.DirectLineConversationService.SetUserConversationWatermark(
                                        this.UserId,
                                        activitySet.Watermark,
                                        cancellationToken);
                            }

                            var incomingNewMessages =
                                activitySet.Activities.Where(m => m.From.Id != this.UserId).ToList();

                            if (incomingNewMessages.Any())
                            {
                                WebSocketUserBotMessenger.Logger.Debug(
                                    $"DirectLine incoming messages for user {this.UserId}:\n{JsonConvert.SerializeObject(incomingNewMessages, Formatting.Indented)}. WebSocketUserBotMessenger: {this.GetHashCode()}. WebSocket: {this.socket.GetHashCode()}, WebSocketState: {this.socket.State}");

                                this.OnBotToUserMessagesReceived(new BotToUserMessagesEventArgs(incomingNewMessages));
                            }
                        }
                    }
                    catch (WebSocketException e)
                    {
                        WebSocketUserBotMessenger.Logger.Error(e.Message);

                        await this.Initialize(cancellationToken);

                        WebSocketUserBotMessenger.Logger.Info($"{this.UserId} Connected after WebSocketException.");
                    }
                }
            }
            finally
            {
                await this.CloseWebSocket(this.socket);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override async Task<bool> Initialize(CancellationToken cancellationToken = default(CancellationToken))
        {
            string watermark =
                (await this.DirectLineConversationService.GetUserConversationInfoAsync(this.UserId, cancellationToken)).Watermark;
            await this.DirectLineConversationService.ReconnectUserConversationAsync(this.UserId, watermark, cancellationToken);
            WebSocketUserBotMessenger.Logger.Info($"{this.UserId} Watermark: {watermark}");

            var socketUri = await this.DirectLineConversationService.GetUserConversationWebSocketUriAsync(this.UserId, cancellationToken);
            WebSocketUserBotMessenger.Logger.Info($"{this.UserId} SocketUri: {socketUri}");

            var connectionTask = this.socket.ConnectAsync(socketUri, cancellationToken);
            return await WebSocketUserBotMessenger.TimeoutAwaitAsync(connectionTask, 3000);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.socket.Dispose();
                this.webSocketCancellationTokenSource.Dispose();
                // Free any other managed objects here.
            }

            // Free any unmanaged objects here.
            this.disposed = true;
        }

        private static async Task<bool> TimeoutAwaitAsync(Task task, int timeoutMs)
        {
            if (await Task.WhenAny(task, Task.Delay(timeoutMs)) == task)
            {
                // task completed within timeout
                return true;
            }
            else
            {
                return false;
                // timeout logic
            }
        }

        private async Task<string> ReadMessageFromSocket()
        {
            string messageReceived = null;
            var bytesReceived = new ArraySegment<byte>(new byte[1024]);

            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await this.socket.ReceiveAsync(bytesReceived, this.webSocketCancellationTokenSource.Token);
                    ms.Write(bytesReceived.Array, bytesReceived.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        messageReceived = reader.ReadToEnd();
                    }
                }
            }

            return messageReceived;
        }

        private async Task CloseWebSocket(ClientWebSocket clientWebSocket)
        {
            if (clientWebSocket != null && clientWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    WebSocketUserBotMessenger.Logger.Error(ex);
                }
            }

            this.socket = null;
        }
    }
}