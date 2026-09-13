using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ContextualBlocks;

public sealed class ContextualBlockPreviewResultDto
{
    public string OperationId { get; set; } = string.Empty;

    public string BlockType { get; set; } = string.Empty;

    public bool IsApplied { get; set; }

    public bool CanApply { get; set; }

    public DateTime PreviewedAtUtc { get; set; }

    public ContextualBlockPreviewTargetDto Target { get; set; } = new ContextualBlockPreviewTargetDto();

    public ContextualBlockPreviewCountsDto Counts { get; set; } = new ContextualBlockPreviewCountsDto();

    public List<ContextualBlockPreviewChangeDto> Changes { get; set; } = new List<ContextualBlockPreviewChangeDto>();

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Errors { get; set; } = new List<string>();
}
