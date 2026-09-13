using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Ports;

public interface ISocialWebhookHandler
{
    SocialNetwork Network { get; }

    bool IsEnabled { get; }

    bool VerifySubscriptionToken(string? verifyToken);

    bool VerifySignature(string payload, string? signature);

    IReadOnlyCollection<SocialWebhookChange> ParseChanges(string payload);
}
