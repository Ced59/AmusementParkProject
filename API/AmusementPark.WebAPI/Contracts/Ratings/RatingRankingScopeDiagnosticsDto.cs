namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingScopeDiagnosticsDto
{
    public string ScopeKey { get; set; } = string.Empty;

    public string TargetFamily { get; set; } = string.Empty;

    public string? ParkItemCategory { get; set; }

    public string MethodologyVersion { get; set; } = string.Empty;

    public string? CurrentSnapshotId { get; set; }

    public DateTime? GeneratedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public long? RebuildDurationMilliseconds { get; set; }

    public int TotalEntryCount { get; set; }

    public int EligibleEntryCount { get; set; }

    public long SourceRevision { get; set; }

    public long? PublishedSourceRevision { get; set; }

    public bool IsRebuildOutstanding { get; set; }

    public bool IsDiagnosticSourceTruncated { get; set; }

    public string? LastJobStatus { get; set; }

    public string? LastErrorCode { get; set; }

    public DateTime? LastJobUpdatedAtUtc { get; set; }
}
