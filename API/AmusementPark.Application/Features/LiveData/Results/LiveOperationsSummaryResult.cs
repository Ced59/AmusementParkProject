namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record LiveOperationsSummaryResult(
    int MappingCount,
    int EligibleMappingCount,
    int CandidateMappingCount,
    int SuspendedMappingCount,
    long PendingIncidentCount,
    bool CoverageTruncated);
