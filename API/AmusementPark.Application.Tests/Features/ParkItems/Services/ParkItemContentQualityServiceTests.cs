using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems.Services;

public sealed class ParkItemContentQualityServiceTests
{
    [Fact]
    public void Evaluate_WhenItemHasRequiredPublicFields_ShouldBePublishable()
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

        ParkItemContentQualityService service = new ParkItemContentQualityService();

        AmusementPark.Application.Features.ParkItems.Results.ParkItemContentQualityResult result = service.Evaluate(item);

        Assert.True(result.IsPublishable);
        Assert.True(result.HasFrenchDescription);
        Assert.True(result.HasEnglishDescription);
        Assert.Empty(result.MissingRequirementKeys);
    }

    [Fact]
    public void Evaluate_WhenItemMissesPublicFields_ShouldExposeMissingRequirementKeys()
    {
        ParkItem item = new ParkItem
        {
            ParkId = "park-1",
            Name = "Draft",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.Attraction,
            Descriptions = new List<LocalizedText>(),
        };

        ParkItemContentQualityService service = new ParkItemContentQualityService();

        AmusementPark.Application.Features.ParkItems.Results.ParkItemContentQualityResult result = service.Evaluate(item);

        Assert.False(result.IsPublishable);
        Assert.Contains("descriptionAny", result.MissingRequirementKeys);
        Assert.Contains("zone", result.MissingRequirementKeys);
        Assert.Contains("typePrecise", result.MissingRequirementKeys);
    }
}
