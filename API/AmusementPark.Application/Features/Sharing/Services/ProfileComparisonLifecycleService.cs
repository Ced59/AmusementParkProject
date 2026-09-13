using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ProfileComparisonLifecycleService
{
    private readonly IProfileComparisonRepository comparisonRepository;
    private readonly TimeProvider timeProvider;

    public ProfileComparisonLifecycleService(
        IProfileComparisonRepository comparisonRepository,
        TimeProvider? timeProvider = null)
    {
        this.comparisonRepository = comparisonRepository
            ?? throw new ArgumentNullException(nameof(comparisonRepository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ProfileComparisonRevocationResult>> RevokeAsync(
        string userId,
        string shareId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = userId?.Trim() ?? string.Empty;
        if (normalizedUserId.Length == 0
            || !ShareToken.TryParse(shareId, out ShareToken shareToken))
        {
            return NotFound();
        }

        ProfileComparison? comparison = await this.comparisonRepository.GetByShareTokenAsync(
            shareToken,
            cancellationToken);
        if (comparison is null || !comparison.HasParticipant(normalizedUserId))
        {
            return NotFound();
        }

        if (!comparison.IsActive)
        {
            return ApplicationResult<ProfileComparisonRevocationResult>.Success(
                new ProfileComparisonRevocationResult(comparison.RevokedAtUtc!.Value));
        }

        long expectedVersion = comparison.Version;
        comparison.Revoke(normalizedUserId, this.timeProvider.GetUtcNow().UtcDateTime);
        ProfileComparisonWriteOutcome outcome = await this.comparisonRepository.ReplaceAsync(
            comparison,
            expectedVersion,
            cancellationToken);
        return outcome == ProfileComparisonWriteOutcome.Success
            ? ApplicationResult<ProfileComparisonRevocationResult>.Success(
                new ProfileComparisonRevocationResult(comparison.RevokedAtUtc!.Value))
            : ApplicationResult<ProfileComparisonRevocationResult>.Failure(
                SharingApplicationErrors.ComparisonChangedConcurrently());
    }

    private static ApplicationResult<ProfileComparisonRevocationResult> NotFound()
    {
        return ApplicationResult<ProfileComparisonRevocationResult>.Failure(
            SharingApplicationErrors.ComparisonNotFound());
    }
}
