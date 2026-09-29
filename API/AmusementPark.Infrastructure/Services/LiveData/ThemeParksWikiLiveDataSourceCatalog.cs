using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Configuration.LiveData;

namespace AmusementPark.Infrastructure.Services.LiveData;

public sealed class ThemeParksWikiLiveDataSourceCatalog : ILiveDataSourceCatalog
{
    private readonly LiveDataSourcePresentation presentation;

    public ThemeParksWikiLiveDataSourceCatalog(LiveDataPollingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        bool isEnabled = settings.Enabled
            && settings.PublicReadEnabled
            && settings.Targets.Any(static target => target.Enabled
                && string.Equals(
                    target.SourceId?.Trim(),
                    "themeparks-wiki",
                    StringComparison.Ordinal));
        this.presentation = new LiveDataSourcePresentation(
            new LiveDataSource(
                LiveDataSourceId.Parse("themeparks-wiki"),
                LiveDataSourceType.AuthorizedAggregator,
                "ThemeParks.wiki",
                new SourceUsagePolicy(
                    "themeparks-wiki-terms-2026-09-28",
                    "https://www.themeparks.wiki/terms",
                    true,
                    true,
                    false,
                    true,
                    "live.source.themeparks-wiki.attribution",
                    new DateTime(2026, 9, 28, 15, 47, 0, DateTimeKind.Utc)),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(30),
                isEnabled ? LiveDataSourceStatus.Active : LiveDataSourceStatus.Suspended),
            100,
            "Powered by ThemeParks.wiki",
            "https://themeparks.wiki/");
    }

    public LiveDataSourcePresentation? Find(LiveDataSourceId sourceId)
    {
        return sourceId == this.presentation.Source.Id ? this.presentation : null;
    }
}
