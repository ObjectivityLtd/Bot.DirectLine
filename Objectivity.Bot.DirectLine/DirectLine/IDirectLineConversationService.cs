namespace Objectivity.Bot.DirectLine.DirectLine
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.DirectLine;
    using Microsoft.Rest;

    public interface IDirectLineConversationService
    {
        Task<ConversationInfo> GetUserConversationInfoAsync(string userId, CancellationToken cancellationToken = default(CancellationToken));

        Task<Uri> GetUserConversationWebSocketUriAsync(string userId, CancellationToken cancellationToken = default(CancellationToken));

        Task ReconnectUserConversationAsync(string userId, string waterMark = null, CancellationToken cancellationToken = default(CancellationToken));

        Task SetUserConversationWatermark(string userId, string watermark, CancellationToken cancellationToken = default(CancellationToken));

        Task PostActivity(string conversationId, Activity activity, CancellationToken cancellationToken);

        Task<HttpOperationResponse<ActivitySet>> GetActivitiesWithHttp(string conversationId, string watermark = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}