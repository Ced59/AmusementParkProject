using AmusementPark.Core.Domain.Identifiers;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Identifiers;

public sealed class IdentifierRulesTests
{
    [Theory]
    [InlineData("entity-1", "entity-1")]
    [InlineData("  legacy:Park_Item/42  ", "legacy:Park_Item/42")]
    [InlineData("01234567-89AB-CDEF-0123-456789ABCDEF", "01234567-89AB-CDEF-0123-456789ABCDEF")]
    [InlineData("0123456789abcdef0123456789abcdef", "0123456789abcdef0123456789abcdef")]
    public void NormalizeRequired_WhenValueIsValid_ShouldTrimWithoutChangingCaseOrFormat(
        string source,
        string expected)
    {
        string value = IdentifierRules.NormalizeRequired(source, "identifier");

        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeRequired_WhenValueIsMissing_ShouldReturnStableErrorCode(string? source)
    {
        IdentifierValidationException exception = Assert.Throws<IdentifierValidationException>(
            () => IdentifierRules.NormalizeRequired(source, "identifier"));

        Assert.Equal(IdentifierErrorCodes.Required, exception.ErrorCode);
        Assert.Equal("identifier", exception.ParamName);
    }

    [Fact]
    public void NormalizeRequired_WhenValueIsTooLong_ShouldReturnStableErrorCode()
    {
        string source = new string('a', IdentifierRules.MaximumLength + 1);

        IdentifierValidationException exception = Assert.Throws<IdentifierValidationException>(
            () => IdentifierRules.NormalizeRequired(source, "identifier"));

        Assert.Equal(IdentifierErrorCodes.TooLong, exception.ErrorCode);
    }

    [Theory]
    [InlineData("identifier\u0000value")]
    [InlineData("identifier\u000Avalue")]
    [InlineData("identifier\u007Fvalue")]
    [InlineData("\u000Aidentifier")]
    [InlineData("identifier\u0009")]
    public void NormalizeRequired_WhenValueContainsAControlCharacter_ShouldReturnStableErrorCode(string source)
    {
        IdentifierValidationException exception = Assert.Throws<IdentifierValidationException>(
            () => IdentifierRules.NormalizeRequired(source, "identifier"));

        Assert.Equal(IdentifierErrorCodes.ControlCharacter, exception.ErrorCode);
    }

    [Fact]
    public void NormalizeRequired_WhenValuesDifferOnlyByCase_ShouldKeepThemDistinct()
    {
        string upper = IdentifierRules.NormalizeRequired("Park-1", "identifier");
        string lower = IdentifierRules.NormalizeRequired("park-1", "identifier");

        Assert.NotEqual(upper, lower);
    }
}
