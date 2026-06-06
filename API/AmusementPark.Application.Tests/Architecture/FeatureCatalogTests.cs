using AmusementPark.Application.Architecture;
using Xunit;

namespace AmusementPark.Application.Tests.Architecture;

public sealed class FeatureCatalogTests
{
    [Fact]
    public void All_WhenRead_ShouldContainExpectedCoreFeatureSlices()
    {
        Assert.Contains(FeatureCatalog.All, static feature => feature.Name == "Countries");
        Assert.Contains(FeatureCatalog.All, static feature => feature.Name == "Parks");
        Assert.Contains(FeatureCatalog.All, static feature => feature.Name == "ParkItems");
        Assert.Contains(FeatureCatalog.All, static feature => feature.Name == "AdminAudit");
    }

    [Fact]
    public void All_WhenRead_ShouldHaveUniqueNames()
    {
        Assert.Equal(FeatureCatalog.All.Count, FeatureCatalog.All.Select(static feature => feature.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void All_WhenRead_ShouldHaveUniqueMigrationPriorities()
    {
        Assert.Equal(FeatureCatalog.All.Count, FeatureCatalog.All.Select(static feature => feature.MigrationPriority).Distinct().Count());
    }

    [Fact]
    public void All_WhenRead_ShouldBeOrderedByMigrationPriority()
    {
        IReadOnlyList<Int32> priorities = FeatureCatalog.All.Select(static feature => feature.MigrationPriority).ToList();

        Assert.Equal(priorities.OrderBy(static priority => priority), priorities);
    }
}
