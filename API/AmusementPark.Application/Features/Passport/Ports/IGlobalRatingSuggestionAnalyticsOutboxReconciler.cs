using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IGlobalRatingSuggestionAnalyticsOutboxReconciler
{
    Task<int> ReconcileBatchAsync(
        int maximumEventCount,
        CancellationToken cancellationToken);
}
