using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkFitCandidatePortfolioReadRepository
    : IParkFitCandidatePortfolioReadRepository
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(10);

    private readonly IMongoCollection<ParkDocument> parks;
    private readonly IMongoCollection<ParkFitOperationalStatusDocument> operationalStatuses;
    private readonly string parksCollectionName;

    public ParkFitCandidatePortfolioReadRepository(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.parks = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.operationalStatuses = database.GetCollection<ParkFitOperationalStatusDocument>(
            settings.ParkFitOperationalStatusesCollectionName);
        this.parksCollectionName = settings.ParksCollectionName;
    }

    public async Task<ParkFitCandidatePortfolio> LoadAsync(
        string? countryCode,
        int maximumActiveCandidateCount,
        CancellationToken cancellationToken)
    {
        if (maximumActiveCandidateCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumActiveCandidateCount));
        }

        long totalCandidateCount = await this.parks.CountDocumentsAsync(
            ParkFitCandidatePortfolioMongoDefinitions.BuildParkCandidateFilter(countryCode),
            new CountOptions { MaxTime = QueryTimeout },
            cancellationToken);
        BsonDocument[] pipeline = ParkFitCandidatePortfolioMongoDefinitions.BuildActivePipeline(
            this.parksCollectionName,
            countryCode,
            maximumActiveCandidateCount);
        BsonDocument root = await this.operationalStatuses
            .Aggregate<BsonDocument>(
                pipeline,
                new AggregateOptions { MaxTime = QueryTimeout })
            .FirstOrDefaultAsync(cancellationToken) ?? new BsonDocument();
        return MapPortfolio(root, totalCandidateCount);
    }

    internal static ParkFitCandidatePortfolio MapPortfolio(
        BsonDocument root,
        long totalCandidateCount)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (totalCandidateCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCandidateCount));
        }

        List<Park> activeCandidates = ReadFacet(root, "activeCandidates")
            .Select(static value => BsonSerializer.Deserialize<ParkDocument>(value.AsBsonDocument))
            .Select(static document => document.ToDomain())
            .ToList();
        IReadOnlyDictionary<string, long> counts = ReadStateCounts(root);
        long activeCount = counts.GetValueOrDefault(ParkFitRecommendationState.Active.ToString());
        long suspendedCount = counts.GetValueOrDefault(
            ParkFitRecommendationState.Suspended.ToString());
        long notActivatedCount = Math.Max(
            0L,
            totalCandidateCount - activeCount - suspendedCount);

        return new ParkFitCandidatePortfolio
        {
            ActiveCandidates = activeCandidates,
            TotalCandidateCount = totalCandidateCount,
            InspectedCandidateCount = activeCandidates.Count,
            OperationallySuspendedCandidateCount = ToBoundedInt(suspendedCount),
            NotActivatedCandidateCount = ToBoundedInt(notActivatedCount),
            CandidatePoolTruncated = activeCount > activeCandidates.Count,
        };
    }

    private static BsonArray ReadFacet(BsonDocument root, string field)
    {
        return root.TryGetValue(field, out BsonValue? value) && value.IsBsonArray
            ? value.AsBsonArray
            : new BsonArray();
    }

    private static IReadOnlyDictionary<string, long> ReadStateCounts(BsonDocument root)
    {
        Dictionary<string, long> counts = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (BsonValue value in ReadFacet(root, "stateCounts"))
        {
            if (!value.IsBsonDocument)
            {
                continue;
            }

            BsonDocument document = value.AsBsonDocument;
            if (document.TryGetValue("_id", out BsonValue? state)
                && state.IsString
                && document.TryGetValue("count", out BsonValue? count)
                && count.IsNumeric)
            {
                counts[state.AsString] = count.ToInt64();
            }
        }

        return counts;
    }

    private static int ToBoundedInt(long value)
    {
        return value >= int.MaxValue ? int.MaxValue : (int)Math.Max(0L, value);
    }
}
