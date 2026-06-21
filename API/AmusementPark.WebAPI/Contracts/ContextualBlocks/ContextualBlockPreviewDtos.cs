using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ContextualBlocks;

public sealed class ContextualBlockPreviewRequestDto
{
    public JsonElement Document { get; set; }
}

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

public sealed class ContextualBlockPreviewTargetDto
{
    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

public sealed class ContextualBlockPreviewCountsDto
{
    public int Created { get; set; }

    public int Updated { get; set; }

    public int Deleted { get; set; }

    public int Unchanged { get; set; }

    public int Warnings { get; set; }

    public int Errors { get; set; }
}

public sealed class ContextualBlockPreviewChangeDto
{
    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public string ChangeType { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }
}
