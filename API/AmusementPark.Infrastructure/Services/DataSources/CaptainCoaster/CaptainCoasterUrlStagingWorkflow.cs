using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using System.Globalization;
using System.Threading.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AmusementPark.Application.Features.Search;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal static class CaptainCoasterUrlStagingWorkflow
{
    internal static async Task StageDiscoveredUrlsAsync(this CaptainCoasterDataSourceProvider provider,
            string sessionId,
            IReadOnlyCollection<CaptainCoasterDiscoveredUrl> discoveredUrls,
            CancellationToken cancellationToken)
    {
        await provider.discoveredUrlsCollection.DeleteManyAsync(item => item.SyncSessionId == sessionId, cancellationToken);

        if (discoveredUrls.Count == 0)
        {
            return;
        }

        List<CaptainCoasterDiscoveredUrlDocument> documents = discoveredUrls
            .Select((item, index) => new CaptainCoasterDiscoveredUrlDocument
            {
                SourceKey = CaptainCoasterDataSourceProvider.SourceKeyValue,
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

        foreach (List<CaptainCoasterDiscoveredUrlDocument> batch in provider.ChunkItems(documents, 500))
        {
            await provider.discoveredUrlsCollection.InsertManyAsync(batch, cancellationToken: cancellationToken);
        }
    }

    internal static async Task<IReadOnlyCollection<CaptainCoasterDiscoveredUrl>> LoadDiscoveredUrlsAsync(this CaptainCoasterDataSourceProvider provider,
        CaptainCoasterSyncSessionDocument session,
        string language,
        CancellationToken cancellationToken)
    {
        List<CaptainCoasterDiscoveredUrlDocument> stagedUrls = await provider.discoveredUrlsCollection
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
