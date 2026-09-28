using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoricalVisitDiagnosticsReader : IHistoricalVisitDiagnosticsReader
{
    private readonly IMongoCollection<UserRideOccurrenceDocument> occurrences;

    public HistoricalVisitDiagnosticsReader(IMongoDatabase database, MongoDbSettings settings)
    {
        this.occurrences = database.GetCollection<UserRideOccurrenceDocument>(
            settings.UserRideOccurrencesCollectionName);
    }

    public async Task<HistoricalVisitDiagnosticCounts> GetCountsAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        if (normalizedParkId.Length == 0)
        {
            throw new ArgumentException("A park identifier is required.", nameof(parkId));
        }

        PipelineDefinition<UserRideOccurrenceDocument, BsonDocument> pipeline =
            PipelineDefinition<UserRideOccurrenceDocument, BsonDocument>.Create(
                BuildPipeline(normalizedParkId));
        BsonDocument? result = await this.occurrences.Aggregate(pipeline)
            .FirstOrDefaultAsync(cancellationToken);
        if (result is null)
        {
            return new HistoricalVisitDiagnosticCounts(0, 0, 0);
        }

        return new HistoricalVisitDiagnosticCounts(
            ReadCount(result, "potentiallyInconsistent"),
            ReadCount(result, "confirmedConflicts"),
            ReadCount(result, "unverified"));
    }

    internal static IReadOnlyCollection<BsonDocument> BuildPipeline(string parkId)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        if (normalizedParkId.Length == 0)
        {
            throw new ArgumentException("A park identifier is required.", nameof(parkId));
        }

        return new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                ["parkId"] = normalizedParkId,
                ["status"] = RideOccurrenceStatus.Completed.ToString(),
                ["deletedAtUtc"] = BsonNull.Value,
                ["historicalConsistency"] = new BsonDocument("$in", new BsonArray
                {
                    HistoricalConsistency.Unverified.ToString(),
                    HistoricalConsistency.ConfirmedConflict.ToString(),
                }),
            }),
            new BsonDocument("$facet", new BsonDocument
            {
                ["potentiallyInconsistent"] = BuildDistinctVisitCountFacet(),
                ["confirmedConflicts"] = BuildDistinctVisitCountFacet(
                    HistoricalConsistency.ConfirmedConflict),
                ["unverified"] = BuildDistinctVisitCountFacet(HistoricalConsistency.Unverified),
            }),
        };
    }

    private static BsonArray BuildDistinctVisitCountFacet(HistoricalConsistency? consistency = null)
    {
        BsonArray stages = new();
        if (consistency.HasValue)
        {
            stages.Add(new BsonDocument(
                "$match",
                new BsonDocument("historicalConsistency", consistency.Value.ToString())));
        }

        stages.Add(new BsonDocument("$group", new BsonDocument("_id", "$visitId")));
        stages.Add(new BsonDocument("$count", "count"));
        return stages;
    }

    private static int ReadCount(BsonDocument result, string facetName)
    {
        BsonArray facet = result.GetValue(facetName, new BsonArray()).AsBsonArray;
        return facet.Count == 0
            ? 0
            : facet[0].AsBsonDocument.GetValue("count", 0).ToInt32();
    }
}
