using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.FactualEvents;

public sealed class FactualChangeEventMutationRequestDto
{
    [Range(1, long.MaxValue)]
    public long ExpectedVersion { get; init; }
}
