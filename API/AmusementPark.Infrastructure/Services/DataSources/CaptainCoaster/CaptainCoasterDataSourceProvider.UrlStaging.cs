using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal sealed partial class CaptainCoasterDataSourceProvider
{
    private async Task StageDiscoveredUrlsAsync(
        string sessionId,
        IReadOnlyCollection<CaptainCoasterDiscoveredUrl> discoveredUrls,
        CancellationToken cancellationToken)
    {
        await this.discoveredUrlsCollection.DeleteManyAsync(item => item.SyncSessionId == sessionId, cancellationToken);

        if (discoveredUrls.Count == 0)
        {
            return;
        }

        List<CaptainCoasterDiscoveredUrlDocument> documents = discoveredUrls
            .Select((item, index) => new CaptainCoasterDiscoveredUrlDocument
            {
                SourceKey = SourceKeyValue,
                SyncSessionId = sessionId,
                CaptainCoasterId = item.CaptainCoasterId,
                Language = item.Language,
                Url = item.Url,
                Sequence = index,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DiscoveredAtUtc = DateTime.UtcNow,
            })
            .ToList();

        foreach (List<CaptainCoasterDiscoveredUrlDocument> batch in ChunkItems(documents, 500))
        {
            await this.discoveredUrlsCollection.InsertManyAsync(batch, cancellationToken: cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<CaptainCoasterDiscoveredUrl>> LoadDiscoveredUrlsAsync(
        CaptainCoasterSyncSessionDocument session,
        string language,
        CancellationToken cancellationToken)
    {
        List<CaptainCoasterDiscoveredUrlDocument> stagedUrls = await this.discoveredUrlsCollection
            .Find(item => item.SyncSessionId == session.Id)
            .SortBy(item => item.Sequence)
            .ToListAsync(cancellationToken);

        if (stagedUrls.Count > 0)
        {
            return stagedUrls
                .Select(item => new CaptainCoasterDiscoveredUrl
                {
                    Url = item.Url,
                    Language = item.Language,
                    CaptainCoasterId = item.CaptainCoasterId,
                    Slug = CaptainCoasterScrapingUrlParser.TryParse(item.Url, language)?.Slug ?? string.Empty,
                })
                .ToList();
        }

        List<string> legacyUrls = session.DiscoveredUrls ?? new List<string>();
        return legacyUrls
            .Select(url => CaptainCoasterScrapingUrlParser.TryParse(url, language))
            .Where(static item => item is not null)
            .Cast<CaptainCoasterDiscoveredUrl>()
            .ToList();
    }
}
