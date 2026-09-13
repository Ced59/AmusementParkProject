using System.Text;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class SeoSitemapSnapshotCacheEntry
{
    public SeoSitemapSnapshotCacheEntry(
        SitemapSnapshot snapshot,
        string sectionsStorageId,
        IReadOnlyDictionary<string, string> legacySectionXmlByKey)
    {
        this.Snapshot = snapshot;
        this.SectionsStorageId = sectionsStorageId;
        this.LegacySectionXmlByKey = legacySectionXmlByKey;
        this.SectionCacheToken = string.IsNullOrWhiteSpace(sectionsStorageId)
            ? $"legacy:{snapshot.GeneratedAtUtc.Ticks}"
            : sectionsStorageId;
    }

    public SitemapSnapshot Snapshot { get; }

    public string SectionsStorageId { get; }

    public IReadOnlyDictionary<string, string> LegacySectionXmlByKey { get; }

    public string SectionCacheToken { get; }
}
