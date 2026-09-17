using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.FactualEvents;

public sealed class RetractFactualChangeEventRequestDto
{
    [Range(1, long.MaxValue)]
    public long ExpectedVersion { get; init; }

    [Required]
    [StringLength(100)]
    public string ReasonCode { get; init; } = string.Empty;
}
