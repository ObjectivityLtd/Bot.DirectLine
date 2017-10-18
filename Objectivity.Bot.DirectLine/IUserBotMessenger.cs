namespace Objectivity.Bot.DirectLine
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUserBotMessenger
    {
        event EventHandler<BotToUserMessagesEventArgs> BotToUserMessagesReceived;

        string UserId { get; set; }

        ManualResetEventSlim MessengerInitialized { get; }

        Task StartListening(CancellationToken cancellationToken = default(CancellationToken));

        void StopListening();

        Task SendUserToBotMessageAsync(string messageText, CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> Initialize(CancellationToken cancellationToken = default(CancellationToken));
    }
}