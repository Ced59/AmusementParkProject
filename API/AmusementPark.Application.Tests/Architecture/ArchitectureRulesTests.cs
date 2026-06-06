using AmusementPark.Application.Architecture;
using Xunit;

namespace AmusementPark.Application.Tests.Architecture;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void All_WhenRead_ShouldContainLayeringRules()
    {
        Assert.Contains(ArchitectureRules.All, static rule => rule.Contains("Core", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ArchitectureRules.All, static rule => rule.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ArchitectureRules.All, static rule => rule.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ArchitectureRules.All, static rule => rule.Contains("WebAPI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void All_WhenRead_ShouldContainNoBlankRule()
    {
        Assert.All(ArchitectureRules.All, static rule => Assert.False(string.IsNullOrWhiteSpace(rule)));
    }
}
