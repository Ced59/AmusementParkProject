namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveOperationsSummaryDto
{
    public int MappingCount { get; set; }

    public int EligibleMappingCount { get; set; }

    public int CandidateMappingCount { get; set; }

    public int SuspendedMappingCount { get; set; }

    public long PendingIncidentCount { get; set; }
}
