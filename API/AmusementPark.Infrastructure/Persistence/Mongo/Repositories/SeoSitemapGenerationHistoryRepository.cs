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

public sealed class SeoSitemapGenerationHistoryRepository : ISeoSitemapGenerationHistoryRepository
{
    private readonly IMongoCollection<SeoSitemapGenerationHistoryDocument> collection;

    public SeoSitemapGenerationHistoryRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<SeoSitemapGenerationHistoryDocument>(settings.SeoSitemapGenerationHistoryCollectionName);
    }

    public async Task WriteAsync(SitemapGenerationHistoryEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        SeoSitemapGenerationHistoryDocument document = new SeoSitemapGenerationHistoryDocument
        {
            Id = entry.Id,
            CreatedAt = entry.StartedAtUtc,
            UpdatedAt = entry.CompletedAtUtc ?? DateTime.UtcNow,
            StartedAtUtc = entry.StartedAtUtc,
            CompletedAtUtc = entry.CompletedAtUtc,
            DurationMs = entry.DurationMs,
            Status = entry.Status.ToString(),
            Trigger = entry.Trigger.ToString(),
            TriggeredByUserId = entry.TriggeredByUserId,
            TriggeredByUserEmail = entry.TriggeredByUserEmail,
            TotalUrlCount = entry.TotalUrlCount,
            Sections = entry.Sections.Select(ToDocument).ToList(),
            Errors = entry.Errors.ToList(),
            IndexNow = ToDocument(entry.IndexNow),
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<SitemapGenerationHistoryEntry>> SearchAsync(PagedQuery paging, CancellationToken cancellationToken)
    {
        int skip = (paging.Page - 1) * paging.PageSize;
        long totalItems = await this.collection.CountDocumentsAsync(Builders<SeoSitemapGenerationHistoryDocument>.Filter.Empty, cancellationToken: cancellationToken);
        List<SeoSitemapGenerationHistoryDocument> documents = await this.collection
            .Find(Builders<SeoSitemapGenerationHistoryDocument>.Filter.Empty)
            .SortByDescending(static document => document.StartedAtUtc)
            .Skip(skip)
            .Limit(paging.PageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<SitemapGenerationHistoryEntry> items = documents.Select(ToModel).ToList();
        return new PagedResult<SitemapGenerationHistoryEntry>(items, paging.Page, paging.PageSize, totalItems);
    }

    private static SeoSitemapSectionStatsDocument ToDocument(SitemapSectionStats value)
    {
        return new SeoSitemapSectionStatsDocument
        {
            Key = value.Key,
            FileName = value.FileName,
            DisplayName = value.DisplayName,
            UrlCount = value.UrlCount,
            LastModifiedUtc = value.LastModifiedUtc,
        };
    }

    private static SeoIndexNowSubmissionDocument ToDocument(IndexNowSubmissionResult value)
    {
        return new SeoIndexNowSubmissionDocument
        {
            WasRequested = value.WasRequested,
            IsEnabled = value.IsEnabled,
            IsSuccess = value.IsSuccess,
            SubmittedUrlCount = value.SubmittedUrlCount,
            AcceptedEndpoints = value.AcceptedEndpoints.ToList(),
            Errors = value.Errors.ToList(),
        };
    }

    private static SitemapGenerationHistoryEntry ToModel(SeoSitemapGenerationHistoryDocument document)
    {
        return new SitemapGenerationHistoryEntry
        {
            Id = document.Id,
            StartedAtUtc = document.StartedAtUtc,
            CompletedAtUtc = document.CompletedAtUtc,
            DurationMs = document.DurationMs,
            Status = Enum.TryParse(document.Status, ignoreCase: true, out SitemapGenerationStatus status) ? status : SitemapGenerationStatus.Failed,
            Trigger = Enum.TryParse(document.Trigger, ignoreCase: true, out SitemapGenerationTrigger trigger) ? trigger : SitemapGenerationTrigger.Manual,
            TriggeredByUserId = document.TriggeredByUserId,
            TriggeredByUserEmail = document.TriggeredByUserEmail,
            TotalUrlCount = document.TotalUrlCount,
            Sections = document.Sections.Select(static value => new SitemapSectionStats(value.Key, value.FileName, value.DisplayName, value.UrlCount, value.LastModifiedUtc)).ToList(),
            Errors = document.Errors.ToList(),
            IndexNow = new IndexNowSubmissionResult
            {
                WasRequested = document.IndexNow.WasRequested,
                IsEnabled = document.IndexNow.IsEnabled,
                IsSuccess = document.IndexNow.IsSuccess,
                SubmittedUrlCount = document.IndexNow.SubmittedUrlCount,
                AcceptedEndpoints = document.IndexNow.AcceptedEndpoints.ToList(),
                Errors = document.IndexNow.Errors.ToList(),
            },
        };
    }
}
