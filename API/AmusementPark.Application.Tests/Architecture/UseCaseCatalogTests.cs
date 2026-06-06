using AmusementPark.Application.Architecture;
using Xunit;

namespace AmusementPark.Application.Tests.Architecture;

public sealed class UseCaseCatalogTests
{
    [Fact]
    public void ByFeature_WhenRead_ShouldExposeUseCasesForEveryFeatureCatalogEntry()
    {
        foreach (FeatureSlice feature in FeatureCatalog.All)
        {
            Assert.True(UseCaseCatalog.ByFeature.ContainsKey(feature.Name), $"Missing use case catalog for {feature.Name}.");
            Assert.NotEmpty(UseCaseCatalog.ByFeature[feature.Name]);
        }
    }

    [Fact]
    public void ByFeature_WhenRead_ShouldContainNoEmptyUseCaseName()
    {
        foreach (IReadOnlyList<String> useCases in UseCaseCatalog.ByFeature.Values)
        {
            Assert.All(useCases, static useCase => Assert.False(string.IsNullOrWhiteSpace(useCase)));
        }
    }

    [Fact]
    public void ByFeature_WhenRead_ShouldIncludeCriticalUserAndSeoUseCases()
    {
        Assert.Contains("RegisterLocalUser", UseCaseCatalog.ByFeature["Users"]);
        Assert.Contains("RefreshToken", UseCaseCatalog.ByFeature["Users"]);
        Assert.Contains("Search", UseCaseCatalog.ByFeature["Search"]);
    }
}
