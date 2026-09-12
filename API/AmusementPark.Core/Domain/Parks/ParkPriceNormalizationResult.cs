namespace AmusementPark.Core.Domain.Parks;

public sealed record ParkPriceNormalizationResult(
    ParkPriceValue Value,
    ParkPriceValidationError? Error);
