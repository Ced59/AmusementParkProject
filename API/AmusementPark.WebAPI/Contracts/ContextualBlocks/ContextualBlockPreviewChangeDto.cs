using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ContextualBlocks;

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
