using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Results;

public sealed class ParkFitDataQualityOperationsResult
{
    public required ParkFitDataQualityAssessment Assessment { get; init; }

    public ParkFitRecommendationState RecommendationState { get; init; }

    public long OperationalRevision { get; init; }

    public DateTime? OperationalUpdatedAtUtc { get; init; }

    public int PendingReportCount { get; init; }

    public IReadOnlyCollection<ParkFitOperationalDecisionResult> RecentDecisions { get; init; } =
        Array.Empty<ParkFitOperationalDecisionResult>();
}
