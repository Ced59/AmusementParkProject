using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using Xunit;

namespace AmusementPark.Application.Tests.Features.AttractionAccessConditionTypes;

public sealed class AttractionAccessConditionTypeKeyNormalizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(" Minimum Height ", "minimum-height")]
    [InlineData("MinimumHeight", "minimum-height")]
    [InlineData("Âge minimum accompagné", "age-minimum-accompagne")]
    [InlineData("back / neck restriction", "back-neck-restriction")]
    [InlineData("--Access__Pass!!Required--", "access-pass-required")]
    [InlineData("123 ABC", "123-abc")]
    public void Normalize_WhenValueProvided_ShouldReturnStableKebabCaseKey(string? input, string expected)
    {
        string result = AttractionAccessConditionTypeKeyNormalizer.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_WhenConsecutiveSeparatorsAreProvided_ShouldCollapseSeparators()
    {
        string result = AttractionAccessConditionTypeKeyNormalizer.Normalize("min    height //// accompanied");

        Assert.Equal("min-height-accompanied", result);
    }
}
