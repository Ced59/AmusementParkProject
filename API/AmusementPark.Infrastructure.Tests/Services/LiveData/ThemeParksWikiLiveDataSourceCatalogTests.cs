using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Configuration.LiveData;
using AmusementPark.Infrastructure.Services.LiveData;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.LiveData;

public sealed class ThemeParksWikiLiveDataSourceCatalogTests
{
    [Fact]
    public void Find_WhenPollingKillSwitchIsOff_ShouldSuspendPublicSource()
    {
        LiveDataPollingSettings settings = new LiveDataPollingSettings
        {
            Enabled = false,
            Targets = new List<LiveDataPollingTargetSettings>
            {
                new LiveDataPollingTargetSettings
                {
                    Enabled = true,
                    SourceId = "themeparks-wiki",
                },
            },
        };
        ThemeParksWikiLiveDataSourceCatalog catalog =
            new ThemeParksWikiLiveDataSourceCatalog(settings);

        AmusementPark.Application.Features.LiveData.Models.LiveDataSourcePresentation? source =
            catalog.Find(LiveDataSourceId.Parse("themeparks-wiki"));

        Assert.Equal(LiveDataSourceStatus.Suspended, source?.Source.Status);
    }

    [Fact]
    public void Find_WhenPollingTargetIsEnabled_ShouldReturnAttribution()
    {
        LiveDataPollingSettings settings = new LiveDataPollingSettings
        {
            Enabled = true,
            PublicReadEnabled = true,
            Targets = new List<LiveDataPollingTargetSettings>
            {
                new LiveDataPollingTargetSettings
                {
                    Enabled = true,
                    SourceId = "themeparks-wiki",
                },
            },
        };
        ThemeParksWikiLiveDataSourceCatalog catalog =
            new ThemeParksWikiLiveDataSourceCatalog(settings);

        AmusementPark.Application.Features.LiveData.Models.LiveDataSourcePresentation? source =
            catalog.Find(LiveDataSourceId.Parse("themeparks-wiki"));

        Assert.Equal(LiveDataSourceStatus.Active, source?.Source.Status);
        Assert.Equal("Powered by ThemeParks.wiki", source?.AttributionText);
        Assert.False(source?.Source.UsagePolicy.RedistributionAllowed);
    }
}
