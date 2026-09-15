using AmusementPark.Core.Domain.FactualEvents;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.FactualEvents;

public sealed class FactValueTests
{
    [Fact]
    public void Factories_ShouldProduceStableCultureIndependentValues()
    {
        DateTime timestamp = new DateTime(2026, 9, 15, 10, 30, 12, DateTimeKind.Utc);

        Assert.Equal("12.5", FactValue.FromDecimal(12.500m, " km ").CanonicalValue);
        Assert.Equal("KM", FactValue.FromDecimal(12.500m, " km ").UnitCode);
        Assert.Equal("2026-09-15", FactValue.FromDate(new DateOnly(2026, 9, 15)).CanonicalValue);
        Assert.Equal(
            "2026-09-15T10:30:12.0000000Z",
            FactValue.FromDateTime(timestamp).CanonicalValue);
        Assert.Equal("EUR", FactValue.FromMoney(49.90m, "eur").UnitCode);
    }

    [Fact]
    public void FromText_ShouldAcceptEditorialContentBeyondIdentifierLength()
    {
        string content = new string('a', 1000);

        FactValue value = FactValue.FromText(content);

        Assert.Equal(content, value.CanonicalValue);
    }

    [Fact]
    public void Restore_ShouldCanonicalizePersistedDecimal()
    {
        FactValue value = FactValue.Restore(FactValueKind.Decimal, "12.500", "meters");

        Assert.Equal(FactValueKind.Decimal, value.Kind);
        Assert.Equal("12.5", value.CanonicalValue);
        Assert.Equal("METERS", value.UnitCode);
    }

    [Fact]
    public void Restore_WithUnitOnBoolean_ShouldRejectInvalidShape()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => FactValue.Restore(FactValueKind.Boolean, "true", "flag"));

        Assert.Equal(FactualEventErrorCodes.InvalidFactValue, exception.Code);
    }

    [Fact]
    public void FromDateTime_WithLocalTimestamp_ShouldRejectAmbiguousValue()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => FactValue.FromDateTime(DateTime.Now));

        Assert.Equal(FactualEventErrorCodes.InvalidFactValue, exception.Code);
    }

    [Theory]
    [InlineData(-1, "EUR")]
    [InlineData(1, "EU")]
    [InlineData(1, "€")]
    public void FromMoney_WithInvalidAmountOrCurrency_ShouldRejectValue(
        int amount,
        string currency)
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => FactValue.FromMoney(amount, currency));

        Assert.Equal(FactualEventErrorCodes.InvalidFactValue, exception.Code);
    }

    [Fact]
    public void Restore_WithMalformedCanonicalValue_ShouldRejectValue()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => FactValue.Restore(FactValueKind.Date, "15/09/2026", null));

        Assert.Equal(FactualEventErrorCodes.InvalidFactValue, exception.Code);
    }
}
