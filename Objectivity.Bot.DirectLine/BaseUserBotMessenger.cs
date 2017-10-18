namespace Objectivity.Bot.DirectLine
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using DirectLine;
    using Microsoft.Bot.Connector.DirectLine;
    using Newtonsoft.Json;
    using NLog;

    public abstract class BaseUserBotMessenger : IUserBotMessenger
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        protected BaseUserBotMessenger(IDirectLineConversationService directLineConversationService)
        {
            this.DirectLineConversationService = directLineConversationService;
            this.MessengerInitialized = new ManualResetEventSlim(false);
        }

        public virtual event EventHandler<BotToUserMessagesEventArgs> BotToUserMessagesReceived;

        public string UserId { get; set; }

        public ManualResetEventSlim MessengerInitialized { get; private set; }

        protected IDirectLineConversationService DirectLineConversationService { get; set; }

        protected bool IsListening { get; set; } = false;

        public async Task SendUserToBotMessageAsync(string messageText, CancellationToken cancellationToken = default(CancellationToken))
        {
            var conversationInfo = await this.DirectLineConversationService.GetUserConversationInfoAsync(this.UserId);
            var conversationId = conversationInfo.ConversationId;
            var activity = this.CreateActivity(messageText);

            var debugRandomNumber = new Random().Next(1000000);
            BaseUserBotMessenger.Logger.Debug($"DirectLine outcoming message from user {this.UserId}:\n{JsonConvert.SerializeObject(activity, Formatting.Indented)}\nDebug random number: {debugRandomNumber}.");
            await this.DirectLineConversationService.PostActivity(conversationId, activity, cancellationToken);
            BaseUserBotMessenger.Logger.Debug($"DirectLine outcoming message sent. Debug random number {debugRandomNumber}.");
        }

        public abstract Task<bool> Initialize(CancellationToken cancellationToken = default(CancellationToken));

        public abstract Task StartListening(CancellationToken cancellationToken = default(CancellationToken));

        public virtual void StopListening()
        {
            this.IsListening = false;
        }

        protected virtual void OnBotToUserMessagesReceived(BotToUserMessagesEventArgs e)
        {
            if (this.IsListening)
            {
                var handler = this.BotToUserMessagesReceived;
                handler?.Invoke(this, e);
            }
        }

        private Activity CreateActivity(string messageText)
        {
            return new Activity
            {
                Text = messageText,
                From = new ChannelAccount { Id = this.UserId },
                Type = "message",
                ChannelId = "directline",
            };
        }
    }
}