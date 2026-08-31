using System.Globalization;
using System.Text.Json;
using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class RatingValueTests
{
    public static TheoryData<byte, decimal> ValidValues => new TheoryData<byte, decimal>
    {
        { 1, 0.5m },
        { 2, 1m },
        { 3, 1.5m },
        { 4, 2m },
        { 5, 2.5m },
        { 6, 3m },
        { 7, 3.5m },
        { 8, 4m },
        { 9, 4.5m },
        { 10, 5m },
    };

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void FromHalfSteps_WhenValueIsValid_ShouldPreserveTheExactScale(byte halfSteps, decimal expected)
    {
        RatingValue rating = RatingValue.FromHalfSteps(halfSteps);

        Assert.Equal(halfSteps, rating.HalfSteps);
        Assert.Equal(expected, rating.DecimalValue);
        Assert.Equal((double)expected, rating.DoubleValue);
    }

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void FromDecimal_WhenValueIsValid_ShouldUseTheExactHalfStep(byte expectedHalfSteps, decimal value)
    {
        RatingValue rating = RatingValue.FromDecimal(value);

        Assert.Equal(expectedHalfSteps, rating.HalfSteps);
    }

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void FromDouble_WhenHistoricalValueIsValid_ShouldUseTheExactHalfStep(byte expectedHalfSteps, decimal value)
    {
        RatingValue rating = RatingValue.FromDouble((double)value);

        Assert.Equal(expectedHalfSteps, rating.HalfSteps);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void FromHalfSteps_WhenValueIsOutsideTheScale_ShouldReturnStableErrorCode(byte halfSteps)
    {
        RatingValueValidationException exception = Assert.Throws<RatingValueValidationException>(
            () => RatingValue.FromHalfSteps(halfSteps));

        Assert.Equal(RatingValueErrorCodes.InvalidValue, exception.ErrorCode);
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(4.25)]
    public void FromDouble_WhenValueDoesNotUseAHalfStep_ShouldReturnStableErrorCode(double value)
    {
        RatingValueValidationException exception = Assert.Throws<RatingValueValidationException>(
            () => RatingValue.FromDouble(value));

        Assert.Equal(RatingValueErrorCodes.InvalidStep, exception.ErrorCode);
    }

    [Theory]
    [InlineData("0.25")]
    [InlineData("4.25")]
    public void FromDecimal_WhenValueDoesNotUseAHalfStep_ShouldReturnStableErrorCode(string source)
    {
        decimal value = decimal.Parse(source, CultureInfo.InvariantCulture);

        RatingValueValidationException exception = Assert.Throws<RatingValueValidationException>(
            () => RatingValue.FromDecimal(value));

        Assert.Equal(RatingValueErrorCodes.InvalidStep, exception.ErrorCode);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(0)]
    [InlineData(5.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FromDouble_WhenValueIsOutsideTheScale_ShouldReturnStableErrorCode(double value)
    {
        RatingValueValidationException exception = Assert.Throws<RatingValueValidationException>(
            () => RatingValue.FromDouble(value));

        Assert.Equal(RatingValueErrorCodes.InvalidValue, exception.ErrorCode);
    }

    [Theory]
    [InlineData(4.499999d)]
    [InlineData(4.500001d)]
    public void TryFromDouble_WhenValueIsCloseButNotEqualToAHalfStep_ShouldRejectWithoutEpsilon(double value)
    {
        bool isValid = RatingValue.TryFromDouble(value, out RatingValue _, out string? errorCode);

        Assert.False(isValid);
        Assert.Equal(RatingValueErrorCodes.InvalidStep, errorCode);
    }

    [Fact]
    public void Value_WhenRatingIsUninitialized_ShouldRejectTheDefaultStruct()
    {
        RatingValue rating = default;

        Assert.Throws<InvalidOperationException>(() => rating.HalfSteps);
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("nl-NL")]
    [InlineData("it-IT")]
    [InlineData("es-ES")]
    [InlineData("pl-PL")]
    [InlineData("pt-PT")]
    public void JsonContractValue_ShouldRemainCultureIndependent(string cultureName)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            string json = JsonSerializer.Serialize(RatingValue.FromHalfSteps(9).DoubleValue);

            Assert.Equal("4.5", json);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
