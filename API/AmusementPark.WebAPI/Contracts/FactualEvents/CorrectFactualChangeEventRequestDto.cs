using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.FactualEvents;

public sealed class CorrectFactualChangeEventRequestDto
{
    [Range(1, long.MaxValue)]
    public long ExpectedVersion { get; init; }

    [Required]
    public string SupersedingEventId { get; init; } = string.Empty;
}
