using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IGlobalRatingSuggestionSourceReader
{
    Task<IReadOnlyCollection<GlobalRatingSuggestionSource>> ReadAsync(
        string userId,
        CancellationToken cancellationToken);
}

public interface IGlobalRatingSuggestionStateRepository
{
    Task<bool> IsEnabledAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<GlobalRatingSuggestionTargetState>> GetStatesAsync(
        string userId,
        IReadOnlyCollection<GlobalRatingSuggestionTargetKey> targets,
        CancellationToken cancellationToken);

    Task SetEnabledAsync(
        string userId,
        bool isEnabled,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken);

    Task<bool> TryRecordInteractionAsync(
        string userId,
        RatingTargetType targetType,
        string targetId,
        DateTime? expectedLastPresentedAtUtc,
        GlobalRatingSuggestionInteractionType interactionType,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken);
}

public interface IGlobalRatingSuggestionAnalyticsOutboxReconciler
{
    Task<int> ReconcileBatchAsync(
        int maximumEventCount,
        CancellationToken cancellationToken);
}
