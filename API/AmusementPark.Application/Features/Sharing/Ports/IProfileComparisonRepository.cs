using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IProfileComparisonRepository
{
    Task<ProfileComparison?> GetByIdAsync(
        ProfileComparisonId comparisonId,
        CancellationToken cancellationToken);

    Task<ProfileComparison?> GetByShareTokenAsync(
        ShareToken shareToken,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProfileComparison>> ListActiveByParticipantAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken);

    Task<ProfileComparisonWriteOutcome> CreateAsync(
        ProfileComparison comparison,
        CancellationToken cancellationToken);

    Task<ProfileComparisonWriteOutcome> ReplaceAsync(
        ProfileComparison comparison,
        long expectedVersion,
        CancellationToken cancellationToken);
}
