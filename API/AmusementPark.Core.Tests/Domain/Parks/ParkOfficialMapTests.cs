using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkOfficialMapTests
{
    [Fact]
    public void IsPubliclyDisplayable_WhenVisiblePdfIsStored_ShouldReturnTrue()
    {
        ParkOfficialMap map = new ParkOfficialMap
        {
            Id = "map-2026",
            Year = 2026,
            Format = ParkOfficialMapFormat.Pdf,
            StorageKey = "official-maps/park-1/map-2026.pdf",
            IsVisible = true,
        };

        Assert.True(map.IsPubliclyDisplayable());
    }

    [Fact]
    public void IsPubliclyDisplayable_WhenImageHasNoAlternativeText_ShouldReturnFalse()
    {
        ParkOfficialMap map = new ParkOfficialMap
        {
            Id = "map-2026",
            Year = 2026,
            Format = ParkOfficialMapFormat.Image,
            DocumentUrl = "https://park.example/map.png",
            IsVisible = true,
        };

        Assert.False(map.IsPubliclyDisplayable());

        map.AlternativeTexts.Add(new LocalizedText("fr", "Plan illustré du parc."));
        Assert.True(map.IsPubliclyDisplayable());
    }
}
