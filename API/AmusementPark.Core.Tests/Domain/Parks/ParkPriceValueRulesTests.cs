using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkPriceValueRulesTests
{
    [Fact]
    public void Normalize_ShouldRequireAnAmountAndClearBoundsForFixedPrices()
    {
        ParkPriceNormalizationResult missingAmount = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = ParkPricingMode.Fixed,
            MinimumAmount = 10m,
            MaximumAmount = 20m,
        });
        ParkPriceNormalizationResult valid = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = ParkPricingMode.Fixed,
            Amount = 49m,
            MinimumAmount = 10m,
            MaximumAmount = 20m,
        });

        Assert.Equal(ParkPriceValidationError.FixedAmountRequired, missingAmount.Error);
        Assert.Null(valid.Error);
        Assert.Equal(49m, valid.Value.Amount);
        Assert.Null(valid.Value.MinimumAmount);
        Assert.Null(valid.Value.MaximumAmount);
    }

    [Fact]
    public void Normalize_ShouldRequireOrderedBoundsAndClearAmountForRangePrices()
    {
        ParkPriceNormalizationResult missingBound = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = ParkPricingMode.Range,
            MinimumAmount = 39m,
        });
        ParkPriceNormalizationResult inverted = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = ParkPricingMode.Range,
            MinimumAmount = 59m,
            MaximumAmount = 39m,
        });
        ParkPriceNormalizationResult valid = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = ParkPricingMode.Range,
            Amount = 99m,
            MinimumAmount = 39m,
            MaximumAmount = 59m,
        });

        Assert.Equal(ParkPriceValidationError.RangeBoundsRequired, missingBound.Error);
        Assert.Equal(ParkPriceValidationError.InvalidRange, inverted.Error);
        Assert.Null(valid.Error);
        Assert.Null(valid.Value.Amount);
    }

    [Fact]
    public void Normalize_ShouldAllowOptionalOrderedBoundsAndClearAmountForDynamicPrices()
    {
        ParkPriceNormalizationResult unbounded = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = ParkPricingMode.Dynamic,
            Amount = 49m,
        });
        ParkPriceNormalizationResult bounded = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = ParkPricingMode.Dynamic,
            MinimumAmount = 39m,
            MaximumAmount = 59m,
        });
        ParkPriceNormalizationResult inverted = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = ParkPricingMode.Dynamic,
            MinimumAmount = 59m,
            MaximumAmount = 39m,
        });

        Assert.Null(unbounded.Error);
        Assert.Null(unbounded.Value.Amount);
        Assert.Null(bounded.Error);
        Assert.Equal(ParkPriceValidationError.InvalidRange, inverted.Error);
    }

    [Theory]
    [InlineData(ParkPricingMode.Fixed, -1d, null, null)]
    [InlineData(ParkPricingMode.Range, null, -1d, 10d)]
    [InlineData(ParkPricingMode.Dynamic, null, 10d, -1d)]
    public void Normalize_ShouldRejectNegativeAmounts(
        ParkPricingMode mode,
        double? amount,
        double? minimumAmount,
        double? maximumAmount)
    {
        ParkPriceNormalizationResult result = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = mode,
            Amount = amount.HasValue ? Convert.ToDecimal(amount.Value) : null,
            MinimumAmount = minimumAmount.HasValue ? Convert.ToDecimal(minimumAmount.Value) : null,
            MaximumAmount = maximumAmount.HasValue ? Convert.ToDecimal(maximumAmount.Value) : null,
        });

        Assert.Equal(ParkPriceValidationError.NegativePrice, result.Error);
    }

    [Fact]
    public void Normalize_ShouldRejectUnknownModes()
    {
        ParkPriceNormalizationResult result = ParkPriceValueRules.Normalize(new ParkPriceValue
        {
            Mode = (ParkPricingMode)99,
        });

        Assert.Equal(ParkPriceValidationError.InvalidMode, result.Error);
    }
}
