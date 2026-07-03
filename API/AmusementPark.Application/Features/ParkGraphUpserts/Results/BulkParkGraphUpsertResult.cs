namespace AmusementPark.Application.Features.ParkGraphUpserts.Results;

public sealed class BulkParkGraphUpsertResult
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString();

    public bool IsApplied { get; set; }

    public bool CanApply { get; set; } = true;

    public DateTime PreviewedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? AppliedAtUtc { get; set; }

    public ParkGraphUpsertCounts Counts { get; set; } = new ParkGraphUpsertCounts();

    public List<BulkParkGraphUpsertParkResult> Parks { get; set; } = new List<BulkParkGraphUpsertParkResult>();

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Errors { get; set; } = new List<string>();
}

public sealed class BulkParkGraphUpsertParkResult
{
    public int Index { get; set; }

    public string? TargetParkId { get; set; }

    public string? TargetParkName { get; set; }

    public ParkGraphUpsertResult Result { get; set; } = new ParkGraphUpsertResult();
}
