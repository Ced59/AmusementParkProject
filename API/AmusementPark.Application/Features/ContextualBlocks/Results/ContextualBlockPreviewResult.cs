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
