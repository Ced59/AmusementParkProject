using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task<Park?> RefreshTargetParkAfterAppliedMergesAsync(
        Park targetPark,
        ParkGraphUpsertMergeSummary mergeSummary,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (!apply)
        {
            return targetPark;
        }

        string refreshedParkId = mergeSummary.ParkIdRemaps.TryGetValue(targetPark.Id, out string? remappedParkId)
            ? remappedParkId
            : targetPark.Id;
        if (string.Equals(refreshedParkId, targetPark.Id, StringComparison.Ordinal)
            && !mergeSummary.ChangedParkIds.Contains(targetPark.Id))
        {
            return targetPark;
        }

        return await this.parkRepository.GetByIdAsync(refreshedParkId, true, cancellationToken);
    }
}
