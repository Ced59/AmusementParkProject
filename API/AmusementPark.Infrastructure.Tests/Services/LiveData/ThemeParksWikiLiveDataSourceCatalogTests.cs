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
                    ExternalEntityId = "external-park-1",
                },
            },
        };
        ThemeParksWikiLiveDataSourceCatalog catalog =
            new ThemeParksWikiLiveDataSourceCatalog(settings);

        AmusementPark.Application.Features.LiveData.Models.LiveDataSourcePresentation? source =
            catalog.Find(LiveDataSourceId.Parse("themeparks-wiki"));

        Assert.False(catalog.IsPublicReadEnabled);
        Assert.False(catalog.IsCollectionEnabled);
        Assert.Null(catalog.PublicPollingTarget);
        Assert.Equal("external-park-1", catalog.ConfiguredPollingTarget?.ExternalEntityId);
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
                    ExternalEntityId = "external-park-1",
                },
            },
        };
        ThemeParksWikiLiveDataSourceCatalog catalog =
            new ThemeParksWikiLiveDataSourceCatalog(settings);

        AmusementPark.Application.Features.LiveData.Models.LiveDataSourcePresentation? source =
            catalog.Find(LiveDataSourceId.Parse("themeparks-wiki"));

        Assert.True(catalog.IsPublicReadEnabled);
        Assert.True(catalog.IsCollectionEnabled);
        Assert.Equal("external-park-1", catalog.PublicPollingTarget?.ExternalEntityId);
        Assert.Equal("external-park-1", catalog.ConfiguredPollingTarget?.ExternalEntityId);
        Assert.Equal(LiveDataSourceStatus.Active, source?.Source.Status);
        Assert.Equal("Powered by ThemeParks.wiki", source?.AttributionText);
        Assert.False(source?.Source.UsagePolicy.RedistributionAllowed);
        Assert.Equal(TimeSpan.FromDays(7), source?.Source.HistoryRetentionPolicy?.RawRetention);
        Assert.Equal(
            TimeSpan.FromDays(400),
            source?.Source.HistoryRetentionPolicy?.AggregateRetention);
    }

    [Fact]
    public void Find_WhenCandidatePollingIsDisabled_ShouldKeepStoredPublicReadsAvailable()
    {
        LiveDataPollingSettings settings = new LiveDataPollingSettings
        {
            Enabled = false,
            PublicReadEnabled = true,
            OperationalMutationsEnabled = false,
            Targets = new List<LiveDataPollingTargetSettings>
            {
                new LiveDataPollingTargetSettings
                {
                    Enabled = true,
                    SourceId = "themeparks-wiki",
                    ExternalEntityId = "external-park-1",
                },
            },
        };
        ThemeParksWikiLiveDataSourceCatalog catalog =
            new ThemeParksWikiLiveDataSourceCatalog(settings);

        AmusementPark.Application.Features.LiveData.Models.LiveDataSourcePresentation? source =
            catalog.Find(LiveDataSourceId.Parse("themeparks-wiki"));

        Assert.False(catalog.IsCollectionEnabled);
        Assert.True(catalog.IsPublicReadEnabled);
        Assert.Equal("external-park-1", catalog.PublicPollingTarget?.ExternalEntityId);
        Assert.Equal(LiveDataSourceStatus.Active, source?.Source.Status);
    }
}
