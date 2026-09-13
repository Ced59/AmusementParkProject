using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class CreatePassportRideOccurrencesBatchRequestDto
{
    public IReadOnlyCollection<CreatePassportRideOccurrenceBatchItemDto?> Items { get; init; } =
        Array.Empty<CreatePassportRideOccurrenceBatchItemDto?>();
}
