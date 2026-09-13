namespace AmusementPark.Application.Features.ParkGraphUpserts.Results;

public sealed class ParkGraphUpsertResult
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString();

    public string Mode { get; set; } = "merge";

    public bool IsApplied { get; set; }

    public bool CanApply { get; set; } = true;

    public DateTime PreviewedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? AppliedAtUtc { get; set; }

    public string? TargetParkId { get; set; }

    public string? TargetParkName { get; set; }

    public string? TargetStandaloneAttractionId { get; set; }

    public string? TargetStandaloneAttractionName { get; set; }

    public ParkGraphUpsertCounts Counts { get; set; } = new ParkGraphUpsertCounts();

    public List<ParkGraphUpsertChange> Changes { get; set; } = new List<ParkGraphUpsertChange>();

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Errors { get; set; } = new List<string>();
}
