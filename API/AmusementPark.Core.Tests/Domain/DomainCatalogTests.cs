using AmusementPark.Core.Domain;
using Xunit;

namespace AmusementPark.Core.Tests.Domain;

public sealed class DomainCatalogTests
{
    [Fact]
    public void ExtractedTypes_WhenRead_ShouldContainKnownAggregateRoots()
    {
        Assert.Contains("Park", DomainCatalog.ExtractedTypes);
        Assert.Contains("ParkItem", DomainCatalog.ExtractedTypes);
        Assert.Contains("User", DomainCatalog.ExtractedTypes);
    }

    [Fact]
    public void ExtractedTypes_WhenRead_ShouldNotContainDuplicateNames()
    {
        Assert.Equal(DomainCatalog.ExtractedTypes.Count, DomainCatalog.ExtractedTypes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ExtractedTypes_WhenRead_ShouldBeOrderedByMigrationCatalogDefinition()
    {
        List<string> extractedTypes = DomainCatalog.ExtractedTypes.ToList();

        Assert.True(extractedTypes.IndexOf("Country") < extractedTypes.IndexOf("User"));
        Assert.True(extractedTypes.IndexOf("Park") < extractedTypes.IndexOf("ParkItem"));
    }
}
