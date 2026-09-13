using System.Text.Json;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.LocalizedContent;

public sealed class LocalizedContentApplyResultDto
{
    public string EntityType { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;

    public IReadOnlyCollection<string> UpdatedFields { get; init; } = Array.Empty<string>();

    public int UpdatedLocalizedValueCount { get; init; }
}
