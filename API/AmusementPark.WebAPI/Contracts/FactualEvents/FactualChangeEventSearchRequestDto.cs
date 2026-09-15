using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.FactualEvents;

public sealed class FactualChangeEventSearchRequestDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int Size { get; init; } = 20;

    public string? Status { get; init; }

    public string? TargetType { get; init; }

    public string? EventType { get; init; }

    public string? Confidence { get; init; }
}
