using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Models;

public sealed class ParkFitCandidatePortfolio
{
    public IReadOnlyCollection<Park> ActiveCandidates { get; init; } = Array.Empty<Park>();

    public long TotalCandidateCount { get; init; }

    public int InspectedCandidateCount { get; init; }

    public int OperationallySuspendedCandidateCount { get; init; }

    public int NotActivatedCandidateCount { get; init; }

    public bool CandidatePoolTruncated { get; init; }
}
