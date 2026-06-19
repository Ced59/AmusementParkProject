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

    public ParkGraphUpsertCounts Counts { get; set; } = new ParkGraphUpsertCounts();

    public List<ParkGraphUpsertChange> Changes { get; set; } = new List<ParkGraphUpsertChange>();

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Errors { get; set; } = new List<string>();
}

public sealed class ParkGraphUpsertCounts
{
    public int Created { get; set; }

    public int Updated { get; set; }

    public int Deleted { get; set; }

    public int Unchanged { get; set; }

    public int Warnings { get; set; }

    public int Errors { get; set; }
}

public sealed class ParkGraphUpsertChange
{
    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? EntityKey { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string ChangeType { get; set; } = "Unchanged";

    public string MatchedBy { get; set; } = "none";

    public List<ParkGraphUpsertFieldChange> Fields { get; set; } = new List<ParkGraphUpsertFieldChange>();
}

public sealed class ParkGraphUpsertFieldChange
{
    public string Field { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }
}
