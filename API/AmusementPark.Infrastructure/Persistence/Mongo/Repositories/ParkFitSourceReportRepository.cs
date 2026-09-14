using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkFitSourceReportRepository : IParkFitSourceReportRepository
{
    private readonly IMongoCollection<ParkFitSourceReportDocument> collection;

    public ParkFitSourceReportRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<ParkFitSourceReportDocument>(
            settings.ParkFitSourceReportsCollectionName);
    }

    public async Task<ParkFitSourceReport?> GetAsync(
        ParkFitSourceReportId reportId,
        CancellationToken cancellationToken)
    {
        ParkFitSourceReportDocument? document = await this.collection
            .Find(ParkFitSourceReportMongoDefinitions.BuildIdFilter(reportId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<PagedResult<ParkFitSourceReport>> SearchAsync(
        ParkFitSourceReportSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ParkFitSourceReportDocument> filter =
            ParkFitSourceReportMongoDefinitions.BuildSearchFilter(criteria);
        long totalItems = await this.collection.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken);
        int skip = checked((int)(((long)criteria.Paging.Page - 1L)
            * criteria.Paging.PageSize));
        List<ParkFitSourceReportDocument> documents = await this.collection
            .Find(filter)
            .SortByDescending(static document => document.SubmittedAtUtc)
            .ThenByDescending(static document => document.Id)
            .Skip(skip)
            .Limit(criteria.Paging.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<ParkFitSourceReport>(
            documents.Select(static document => document.ToDomain()).ToArray(),
            criteria.Paging.Page,
            criteria.Paging.PageSize,
            totalItems);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountPendingByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        if (parkIds.Count == 0)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkFitSourceReportDocument> filter =
            Builders<ParkFitSourceReportDocument>.Filter.In(
                static document => document.ParkId,
                parkIds)
            & Builders<ParkFitSourceReportDocument>.Filter.Eq(
                static document => document.Status,
                ParkFitSourceReportStatus.Pending);
        BsonDocument group = new BsonDocument
        {
            { "_id", "$parkId" },
            { "count", new BsonDocument("$sum", 1) },
        };
        List<BsonDocument> counts = await this.collection.Aggregate()
            .Match(filter)
            .Group(group)
            .ToListAsync(cancellationToken);
        return counts
            .Where(static count => count.TryGetValue("_id", out BsonValue? value)
                && value.IsString)
            .ToDictionary(
                static count => count["_id"].AsString,
                static count => count["count"].ToInt32(),
                StringComparer.Ordinal);
    }

    public async Task<ParkFitSourceReportWriteOutcome> CreateAsync(
        ParkFitSourceReport report,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        try
        {
            await this.collection.InsertOneAsync(
                report.ToDocument(),
                cancellationToken: cancellationToken);
            return ParkFitSourceReportWriteOutcome.Success;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ParkFitSourceReportWriteOutcome.Conflict;
        }
    }

    public async Task<ParkFitSourceReportWriteOutcome> ReplaceAsync(
        ParkFitSourceReport report,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            ParkFitSourceReportMongoDefinitions.BuildRevisionFilter(
                report.Id.Value,
                expectedRevision),
            report.ToDocument(),
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1
            ? ParkFitSourceReportWriteOutcome.Success
            : ParkFitSourceReportWriteOutcome.Conflict;
    }
}
