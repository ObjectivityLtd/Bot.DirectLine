namespace Objectivity.Bot.DirectLine.Messenger
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Objectivity.Bot.DirectLine.DirectLine;

    public class UserBotMessengerService : IUserBotMessengerService
    {
        /// <summary>
        /// Dictionary of messangers indexed by userId
        /// </summary>
        private readonly IDictionary<string, IUserBotMessenger> userBotMessengers =
            new Dictionary<string, IUserBotMessenger>();

        private readonly IDirectLineConversationService directLineConversationService;

        public UserBotMessengerService(IDirectLineConversationService directLineConversationService)
        {
            this.directLineConversationService = directLineConversationService;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "todo[sk]")]
        public IUserBotMessenger GetOrCreateMessengerForUser(string userId)
        {
            IUserBotMessenger messenger;
            if (this.userBotMessengers.TryGetValue(userId, out messenger))
            {
                return messenger;
            }

            this.userBotMessengers[userId] = new WebSocketUserBotMessenger(this.directLineConversationService)
            {
                UserId = userId,
            };
            return this.userBotMessengers[userId];
        }

        public async Task StartMessagingForUser(string userId, EventHandler<BotToUserMessagesEventArgs> botToUserMessagesReceivedHandler, CancellationToken cancellationToken = default(CancellationToken))
        {
            var messenger = this.GetOrCreateMessengerForUser(userId);

            messenger.BotToUserMessagesReceived += botToUserMessagesReceivedHandler;

            await messenger.Initialize(cancellationToken);

            var notAwaitedTask = messenger.StartListening(cancellationToken);
        }

        public void StopMessagingForUser(string userId)
        {
            var messenger = this.GetMessengerForUser(userId);
            if (messenger != null)
            {
                messenger.StopListening();
                this.userBotMessengers.Remove(userId);
                (messenger as IDisposable)?.Dispose();
            }
        }

        private IUserBotMessenger GetMessengerForUser(string userId)
        {
            IUserBotMessenger messenger;
            if (this.userBotMessengers.TryGetValue(userId, out messenger))
            {
                return messenger;
            }

            return null;
        }
    }
}
