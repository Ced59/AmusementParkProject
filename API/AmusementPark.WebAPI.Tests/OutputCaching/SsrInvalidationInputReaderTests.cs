using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.OutputCaching;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class SsrInvalidationInputReaderTests
{
    [Fact]
    public void NormalizePaths_ShouldTrimPrefixAndDeduplicate()
    {
        IReadOnlyCollection<string> paths = SsrInvalidationInputReader.NormalizePaths(
            new[] { " fr/parks ", "/fr/parks", " ", "/fr/park/id" }).ToList();

        Assert.Equal(new[] { "/fr/parks", "/fr/park/id" }, paths);
    }

    [Fact]
    public void GetProperties_ShouldBeCaseInsensitiveAndNormalizeCollections()
    {
        InputDto source = new InputDto
        {
            ParkId = "park-id",
            IsVisible = true,
            Ids = new[] { " first ", "", "first", "second" }
        };

        Assert.Equal("park-id", SsrInvalidationInputReader.GetStringProperty(source, "parkid"));
        Assert.True(SsrInvalidationInputReader.GetNullableBooleanProperty(source, "ISVISIBLE"));
        Assert.Equal(
            new[] { "first", "second" },
            SsrInvalidationInputReader.GetStringCollectionProperty(source, "ids"));
    }

    [Theory]
    [InlineData("theme-park", "themepark")]
    [InlineData("STANDALONE_ATTRACTION", "standaloneattraction")]
    public void NormalizeEntityType_ShouldRemoveTransportSeparators(string input, string expected)
    {
        Assert.Equal(expected, SsrInvalidationInputReader.NormalizeEntityType(input));
    }

    [Fact]
    public void ParseEnum_ShouldIgnoreCaseAndRejectUnknownValues()
    {
        Assert.Equal(ParkType.ThemePark, SsrInvalidationInputReader.ParseEnum<ParkType>("themepark"));
        Assert.Null(SsrInvalidationInputReader.ParseEnum<ParkType>("unknown"));
    }

    private sealed class InputDto
    {
        public string ParkId { get; init; } = string.Empty;

        public bool IsVisible { get; init; }

        public IReadOnlyCollection<string> Ids { get; init; } = Array.Empty<string>();
    }
}
