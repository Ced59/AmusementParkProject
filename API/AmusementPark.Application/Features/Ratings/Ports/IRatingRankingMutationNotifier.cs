using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingMutationNotifier
{
    Task NotifyMutationAsync(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory,
        CancellationToken cancellationToken);
}
