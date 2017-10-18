namespace Objectivity.Bot.DirectLine
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DirectLine;
    using Microsoft.Bot.Connector.DirectLine;
    using NLog;

    public class HttpUserBotMessenger : BaseUserBotMessenger
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public HttpUserBotMessenger(IDirectLineConversationService directLineConversationService)
            : base(directLineConversationService)
        {
        }

        public override async Task<bool> Initialize(CancellationToken cancellationToken = default(CancellationToken))
        {
            return true;
        }

        public override async Task StartListening(CancellationToken cancellationToken = default(CancellationToken))
        {
            this.IsListening = true;

            string watermark = (await this.DirectLineConversationService.GetUserConversationInfoAsync(this.UserId, cancellationToken)).Watermark;

            this.MessengerInitialized.Set();

            while (this.IsListening)
            {
                var conversationInfo = await this.DirectLineConversationService.GetUserConversationInfoAsync(this.UserId, cancellationToken);
                var conversationId = conversationInfo.ConversationId;

                var httpResponse = await this.DirectLineConversationService.GetActivitiesWithHttp(conversationId, watermark, cancellationToken);

                ActivitySet responseBody = httpResponse.Body;

                var allNewMessages = responseBody.Activities;
                var incomingNewMessages = allNewMessages.Where(m => m.From.Id != this.UserId).ToList();

                watermark = responseBody.Watermark;
                await this.DirectLineConversationService.SetUserConversationWatermark(this.UserId, watermark);

                HttpUserBotMessenger.Logger.Trace($"[{this.UserId}]\tincoming messages: {incomingNewMessages.Count}");

                this.OnBotToUserMessagesReceived(new BotToUserMessagesEventArgs(incomingNewMessages));

                Thread.Sleep(1000);
            }
        }
    }
}
