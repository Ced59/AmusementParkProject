using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed record SocialPublicationRecovery(
    bool IsRecovered,
    SocialPublication? Publication);
