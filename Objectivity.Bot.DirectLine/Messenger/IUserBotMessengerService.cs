namespace Objectivity.Bot.DirectLine.Messenger
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUserBotMessengerService
    {
        IUserBotMessenger GetOrCreateMessengerForUser(string userId);

        Task StartMessagingForUser(
            string userId,
            EventHandler<BotToUserMessagesEventArgs> botToUserMessagesReceivedHandler,
            CancellationToken cancellationToken = default(CancellationToken));

        void StopMessagingForUser(string userId);
    }
}
