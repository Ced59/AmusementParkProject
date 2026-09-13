using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ShareModerationReportRepository : IShareModerationReportRepository
{
    private readonly IMongoCollection<ShareModerationReportDocument> collection;

    public ShareModerationReportRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<ShareModerationReportDocument>(
            settings.ShareModerationReportsCollectionName);
    }

    public async Task<ShareModerationReport?> GetAsync(
        ShareModerationReportId reportId,
        CancellationToken cancellationToken)
    {
        ShareModerationReportDocument? document = await this.collection
            .Find(ShareModerationReportMongoDefinitions.BuildIdFilter(reportId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<PagedResult<ShareModerationReport>> SearchAsync(
        ShareModerationReportSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ShareModerationReportDocument> filter =
            ShareModerationReportMongoDefinitions.BuildSearchFilter(
                criteria.Status,
                criteria.TargetType,
                criteria.Reason);
        long totalItems = await this.collection.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken);
        int skip = checked((int)(((long)criteria.Paging.Page - 1L)
            * criteria.Paging.PageSize));
        List<ShareModerationReportDocument> documents = await this.collection
            .Find(filter)
            .SortByDescending(static document => document.SubmittedAtUtc)
            .ThenByDescending(static document => document.Id)
            .Skip(skip)
            .Limit(criteria.Paging.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<ShareModerationReport>(
            documents.Select(static document => document.ToDomain()).ToArray(),
            criteria.Paging.Page,
            criteria.Paging.PageSize,
            totalItems);
    }

    public async Task<ShareModerationReportWriteOutcome> CreateAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        try
        {
            await this.collection.InsertOneAsync(
                report.ToDocument(),
                cancellationToken: cancellationToken);
            return ShareModerationReportWriteOutcome.Success;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ShareModerationReportWriteOutcome.Conflict;
        }
    }

    public async Task<ShareModerationReportWriteOutcome> ReplaceAsync(
        ShareModerationReport report,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (expectedVersion < 0
            || expectedVersion == long.MaxValue
            || report.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The report must be exactly one version ahead of the expected version.",
                nameof(report));
        }

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            ShareModerationReportMongoDefinitions.BuildVersionFilter(
                report.Id.Value,
                expectedVersion),
            report.ToDocument(),
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1
            ? ShareModerationReportWriteOutcome.Success
            : ShareModerationReportWriteOutcome.Conflict;
    }
}
