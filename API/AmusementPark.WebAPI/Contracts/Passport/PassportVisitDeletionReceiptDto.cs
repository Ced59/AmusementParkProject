namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportVisitDeletionReceiptDto
{
    public string VisitId { get; init; } = string.Empty;

    public DateTime DeletedAtUtc { get; init; }

    public DateTime PurgeScheduledForUtc { get; init; }
}
