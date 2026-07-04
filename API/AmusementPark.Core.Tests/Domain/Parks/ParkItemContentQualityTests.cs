using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkItemContentQualityTests
{
    [Fact]
    public void EvaluateContentQuality_WhenItemHasRequiredPublicFields_ShouldBePublishable()
    {
        ParkItem item = new ParkItem
        {
            ParkId = "park-1",
            ZoneId = "zone-1",
            Name = "Coaster",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("fr", "Description FR"),
                new LocalizedText("en", "Description EN"),
            },
        };

        ParkItemContentQuality result = item.EvaluateContentQuality();

        Assert.True(result.IsPublishable);
        Assert.True(result.HasFrenchDescription);
        Assert.True(result.HasEnglishDescription);
        Assert.Empty(result.MissingRequirementKeys);
    }

    [Fact]
    public void EvaluateContentQuality_WhenItemHasContentButNoZone_ShouldRemainPublishableWithMissingZoneSignal()
    {
        ParkItem item = new ParkItem
        {
            ParkId = "park-1",
            Name = "Restrooms",
            Category = ParkItemCategory.Service,
            Type = ParkItemType.Toilets,
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("fr", "Toilettes publiques"),
                new LocalizedText("en", "Public restrooms"),
            },
        };

        ParkItemContentQuality result = item.EvaluateContentQuality();

        Assert.True(result.IsPublishable);
        Assert.Contains("zone", result.MissingRequirementKeys);
        Assert.DoesNotContain("descriptionAny", result.MissingRequirementKeys);
        Assert.DoesNotContain("typePrecise", result.MissingRequirementKeys);
    }

    [Fact]
    public void EvaluateContentQuality_WhenItemMissesPublicFields_ShouldExposeMissingRequirementKeys()
    {
        ParkItem item = new ParkItem
        {
            ParkId = "park-1",
            Name = "Draft",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.Attraction,
            Descriptions = new List<LocalizedText>(),
        };

        ParkItemContentQuality result = item.EvaluateContentQuality();

        Assert.False(result.IsPublishable);
        Assert.Contains("descriptionAny", result.MissingRequirementKeys);
        Assert.Contains("zone", result.MissingRequirementKeys);
        Assert.Contains("typePrecise", result.MissingRequirementKeys);
    }
}
