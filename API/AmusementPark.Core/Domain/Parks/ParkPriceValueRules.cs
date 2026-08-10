namespace AmusementPark.Core.Domain.Parks;

public enum ParkPriceValidationError
{
    InvalidMode = 0,
    NegativePrice = 1,
    FixedAmountRequired = 2,
    RangeBoundsRequired = 3,
    InvalidRange = 4,
}

public sealed record ParkPriceNormalizationResult(
    ParkPriceValue Value,
    ParkPriceValidationError? Error);

public static class ParkPriceValueRules
{
    public static ParkPriceNormalizationResult Normalize(ParkPriceValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        ParkPriceValue normalized = new ParkPriceValue
        {
            Mode = value.Mode,
            Amount = value.Amount,
            MinimumAmount = value.MinimumAmount,
            MaximumAmount = value.MaximumAmount,
        };

        if (!Enum.IsDefined(normalized.Mode))
        {
            return new ParkPriceNormalizationResult(normalized, ParkPriceValidationError.InvalidMode);
        }

        if (normalized.Amount < 0 || normalized.MinimumAmount < 0 || normalized.MaximumAmount < 0)
        {
            return new ParkPriceNormalizationResult(normalized, ParkPriceValidationError.NegativePrice);
        }

        switch (normalized.Mode)
        {
            case ParkPricingMode.Fixed:
                normalized.MinimumAmount = null;
                normalized.MaximumAmount = null;
                return new ParkPriceNormalizationResult(
                    normalized,
                    normalized.Amount.HasValue ? null : ParkPriceValidationError.FixedAmountRequired);

            case ParkPricingMode.Range:
                normalized.Amount = null;
                if (!normalized.MinimumAmount.HasValue || !normalized.MaximumAmount.HasValue)
                {
                    return new ParkPriceNormalizationResult(normalized, ParkPriceValidationError.RangeBoundsRequired);
                }

                return new ParkPriceNormalizationResult(
                    normalized,
                    normalized.MinimumAmount.Value <= normalized.MaximumAmount.Value
                        ? null
                        : ParkPriceValidationError.InvalidRange);

            case ParkPricingMode.Dynamic:
                normalized.Amount = null;
                return new ParkPriceNormalizationResult(
                    normalized,
                    normalized.MinimumAmount.HasValue
                        && normalized.MaximumAmount.HasValue
                        && normalized.MinimumAmount.Value > normalized.MaximumAmount.Value
                            ? ParkPriceValidationError.InvalidRange
                            : null);

            default:
                return new ParkPriceNormalizationResult(normalized, ParkPriceValidationError.InvalidMode);
        }
    }
}
