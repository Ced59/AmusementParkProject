using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.HistoricalExistenceReports.Models;
using AmusementPark.Application.Features.HistoricalExistenceReports.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoricalExistenceReportRepository
    : IHistoricalExistenceReportRepository
{
    private readonly IMongoCollection<HistoricalExistenceReportDocument> collection;

    public HistoricalExistenceReportRepository(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<HistoricalExistenceReportDocument>(
            settings.HistoricalExistenceReportsCollectionName);
    }

    public async Task<HistoricalExistenceReport?> GetAsync(
        HistoricalExistenceReportId reportId,
        CancellationToken cancellationToken)
    {
        HistoricalExistenceReportDocument? document = await this.collection
            .Find(HistoricalExistenceReportMongoDefinitions.BuildIdFilter(reportId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<HistoricalExistenceReport>> ListOwnedByVisitAsync(
        string ownerUserId,
        VisitId visitId,
        CancellationToken cancellationToken)
    {
        List<HistoricalExistenceReportDocument> documents = await this.collection
            .Find(HistoricalExistenceReportMongoDefinitions.BuildOwnedVisitFilter(
                ownerUserId,
                visitId.Value))
            .SortByDescending(static document => document.SubmittedAtUtc)
            .ThenByDescending(static document => document.Id)
            .Limit(100)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<PagedResult<HistoricalExistenceReport>> SearchAsync(
        HistoricalExistenceReportSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        FilterDefinition<HistoricalExistenceReportDocument> filter =
            HistoricalExistenceReportMongoDefinitions.BuildSearchFilter(criteria);
        long totalItems = await this.collection.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken);
        int skip = checked((int)(((long)criteria.Paging.Page - 1L)
            * criteria.Paging.PageSize));
        List<HistoricalExistenceReportDocument> documents = await this.collection
            .Find(filter)
            .SortByDescending(static document => document.SubmittedAtUtc)
            .ThenByDescending(static document => document.Id)
            .Skip(skip)
            .Limit(criteria.Paging.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<HistoricalExistenceReport>(
            documents.Select(static document => document.ToDomain()).ToArray(),
            criteria.Paging.Page,
            criteria.Paging.PageSize,
            totalItems);
    }

    public async Task<HistoricalExistenceReportWriteOutcome> CreateAsync(
        HistoricalExistenceReport report,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        try
        {
            await this.collection.InsertOneAsync(
                report.ToDocument(),
                cancellationToken: cancellationToken);
            return HistoricalExistenceReportWriteOutcome.Success;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return HistoricalExistenceReportWriteOutcome.Conflict;
        }
    }

    public async Task<HistoricalExistenceReportWriteOutcome> ReplaceAsync(
        HistoricalExistenceReport report,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            HistoricalExistenceReportMongoDefinitions.BuildRevisionFilter(
                report.Id.Value,
                expectedRevision),
            report.ToDocument(),
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1
            ? HistoricalExistenceReportWriteOutcome.Success
            : HistoricalExistenceReportWriteOutcome.Conflict;
    }
}
