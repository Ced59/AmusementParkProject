using AmusementPark.Infrastructure.Persistence.Mongo.Projections;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Projections;

public sealed class StandaloneSearchProjectionBackfillTests
{
    [Fact]
    public void BuildProjectionOriginalIds_ShouldTrimSkipEmptyAndDeduplicate()
    {
        string?[] standaloneAttractionIds =
        {
            " standalone-1 ",
            "",
            "standalone-1",
            "standalone-2",
            " ",
        };
        string[] expectedOriginalIds = { "standaloneAttraction_standalone-1", "standaloneAttraction_standalone-2" };

        IReadOnlyCollection<string> originalIds = StandaloneSearchProjectionBackfill.BuildProjectionOriginalIds(standaloneAttractionIds);

        Assert.Equal(expectedOriginalIds, originalIds);
    }

    [Fact]
    public void ResolveMissingStandaloneAttractionIds_ShouldReturnOnlyIdsWithoutProjection()
    {
        string?[] standaloneAttractionIds = { " standalone-1 ", "standalone-2", "standalone-2", "standalone-3", "" };
        string?[] existingProjectionOriginalIds = { "standaloneAttraction_standalone-2", "park_legacy", "standaloneAttraction_ " };
        string[] expectedMissingIds = { "standalone-1", "standalone-3" };

        IReadOnlyCollection<string> missingIds = StandaloneSearchProjectionBackfill.ResolveMissingStandaloneAttractionIds(
            standaloneAttractionIds,
            existingProjectionOriginalIds);

        Assert.Equal(expectedMissingIds, missingIds);
    }
}
