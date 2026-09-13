using System.Text.Json;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.LocalizedContent;

public sealed class LocalizedContentTargetDto
{
    public string EntityType { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string? Context { get; init; }

    public IReadOnlyCollection<string> SupportedFields { get; init; } = Array.Empty<string>();
}
