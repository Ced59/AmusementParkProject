namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportVisitDeletionPreviewDto
{
    public string VisitId { get; init; } = string.Empty;

    public long ExpectedVersion { get; init; }

    public long OccurrenceCount { get; init; }

    public long AssessmentCount { get; init; }

    public int RetentionDays { get; init; }
}
