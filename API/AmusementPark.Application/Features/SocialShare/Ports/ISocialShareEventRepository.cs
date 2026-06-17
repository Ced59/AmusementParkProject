using AmusementPark.Application.Features.SocialShare.Contracts;
using AmusementPark.Core.Domain.SocialShare;

namespace AmusementPark.Application.Features.SocialShare.Ports;

public interface ISocialShareEventRepository
{
    Task<SocialShareEvent> CreateAsync(SocialShareEvent shareEvent, CancellationToken cancellationToken);

    Task<SocialShareStatsResult> GetStatsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
