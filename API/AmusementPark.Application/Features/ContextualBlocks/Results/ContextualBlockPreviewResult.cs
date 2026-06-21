namespace AmusementPark.Application.Features.ContextualBlocks.Results;

public sealed class ContextualBlockPreviewResult
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString();

    public string BlockType { get; set; } = string.Empty;

    public bool IsApplied { get; set; }

    public bool CanApply { get; set; } = true;

    public DateTime PreviewedAtUtc { get; set; } = DateTime.UtcNow;

    public ContextualBlockPreviewTarget Target { get; set; } = new ContextualBlockPreviewTarget();

    public ContextualBlockPreviewCounts Counts { get; set; } = new ContextualBlockPreviewCounts();

    public List<ContextualBlockPreviewChange> Changes { get; set; } = new List<ContextualBlockPreviewChange>();

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Errors { get; set; } = new List<string>();
}

public sealed class ContextualBlockPreviewTarget
{
    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

public sealed class ContextualBlockPreviewCounts
{
    public int Created { get; set; }

    public int Updated { get; set; }

    public int Deleted { get; set; }

    public int Unchanged { get; set; }

    public int Warnings { get; set; }

    public int Errors { get; set; }
}

public sealed class ContextualBlockPreviewChange
{
    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public string ChangeType { get; set; } = "Unchanged";

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }
}
