namespace Objectivity.Bot.DirectLine.DirectLine
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.DirectLine;
    using Microsoft.Rest;

    public class DirectLineConversationService : IDirectLineConversationService
    {
        private static readonly IDictionary<string, Conversation> Conversations = new Dictionary<string, Conversation>();
        private static readonly IDictionary<string, string> ConversationWatermakrs = new Dictionary<string, string>();

        private readonly IDirectLineClient directLineClient;

        public DirectLineConversationService(IDirectLineClient directLineClient)
        {
            this.directLineClient = directLineClient;
        }

        public async Task<ConversationInfo> GetUserConversationInfoAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var conversation = await this.GetOrCreateConversationAsync(userId, cancellationToken);
            var conversationId = conversation.ConversationId;
            string watermark;
            DirectLineConversationService.ConversationWatermakrs.TryGetValue(conversationId, out watermark);

            return new ConversationInfo { ConversationId = conversationId, Watermark = watermark, };
        }

        public async Task<Uri> GetUserConversationWebSocketUriAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var conversation = await this.GetOrCreateConversationAsync(userId, cancellationToken);
            return new Uri(conversation.StreamUrl);
        }

        public async Task ReconnectUserConversationAsync(string userId, string waterMark = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var conversation = await this.GetOrCreateConversationAsync(userId, cancellationToken);
            var reconnectedConversation = await this.ReconnectConversationAsync(conversation.ConversationId, waterMark, cancellationToken);
            DirectLineConversationService.Conversations[userId] = reconnectedConversation;
        }

        public async Task SetUserConversationWatermark(string userId, string watermark, CancellationToken cancellationToken = default(CancellationToken))
        {
            var conversation = await this.GetOrCreateConversationAsync(userId, cancellationToken);
            DirectLineConversationService.ConversationWatermakrs[conversation.ConversationId] = watermark;
        }

        public async Task PostActivity(string conversationId, Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.directLineClient.Conversations.PostActivityWithHttpMessagesAsync(conversationId, activity, cancellationToken: cancellationToken);
        }

        public async Task<HttpOperationResponse<ActivitySet>> GetActivitiesWithHttp(string conversationId, string watermark = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.directLineClient.Conversations.GetActivitiesWithHttpMessagesAsync(conversationId, watermark, cancellationToken: cancellationToken);
        }

        private async Task<Conversation> GetOrCreateConversationAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            Conversation conversation;
            if (DirectLineConversationService.Conversations.TryGetValue(userId, out conversation))
            {
                return conversation;
            }

            conversation = await this.CreateNewConversationAsync(cancellationToken);
            DirectLineConversationService.Conversations[userId] = conversation;
            return conversation;
        }

        private async Task<Conversation> CreateNewConversationAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await this.directLineClient.Conversations.StartConversationWithHttpMessagesAsync(cancellationToken: cancellationToken);

            if (response.Response.StatusCode != HttpStatusCode.Created)
            {
                throw new ApplicationException("Could not create conversation.");
            }

            return response.Body;
        }

        private async Task<Conversation> ReconnectConversationAsync(string conversationId, string watermark = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await this.directLineClient.Conversations.ReconnectToConversationWithHttpMessagesAsync(conversationId, watermark, cancellationToken: cancellationToken);
            return response.Body;
        }
    }
}