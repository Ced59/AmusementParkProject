using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo spécialisé pour le contrat public léger ParkDetailSummary.
/// </summary>
public sealed class ParkDetailSummaryReadRepository : IParkDetailSummaryReadRepository
{
    private readonly IMongoCollection<ParkDocument> parksCollection;
    private readonly IMongoCollection<ParkItemDocument> parkItemsCollection;
    private readonly IMongoCollection<ParkZoneDocument> parkZonesCollection;
    private readonly IMongoCollection<ImageDocument> imagesCollection;
    private readonly IMongoCollection<ParkFounderDocument> parkFoundersCollection;
    private readonly IMongoCollection<ParkOperatorDocument> parkOperatorsCollection;
    private readonly IMongoCollection<RatingAggregateDocument> ratingAggregatesCollection;

    public ParkDetailSummaryReadRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.parksCollection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItemsCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
        this.parkZonesCollection = database.GetCollection<ParkZoneDocument>(settings.ParkZonesCollectionName);
        this.imagesCollection = database.GetCollection<ImageDocument>(settings.ImagesCollectionName);
        this.parkFoundersCollection = database.GetCollection<ParkFounderDocument>(settings.ParkFoundersCollectionName);
        this.parkOperatorsCollection = database.GetCollection<ParkOperatorDocument>(settings.ParkOperatorsCollectionName);
        this.ratingAggregatesCollection = database.GetCollection<RatingAggregateDocument>(settings.RatingAggregatesCollectionName);
    }

    public async Task<ParkDetailSummaryResult?> GetAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> parkFilter = Builders<ParkDocument>.Filter.Eq(document => document.Id, parkId);
        if (!includeHidden)
        {
            parkFilter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        ParkDocument? parkDocument = await this.parksCollection.Find(parkFilter).FirstOrDefaultAsync(cancellationToken);
        if (parkDocument is null)
        {
            return null;
        }

        Task<IReadOnlyDictionary<ParkItemCategory, int>> countsTask = this.GetCountsByCategoryAsync(parkId, includeHidden, closedFilter, cancellationToken);
        Task<int> zoneCountTask = this.GetZoneCountAsync(parkId, includeHidden, cancellationToken);
        Task<Image?> mainImageTask = this.GetMainImageAsync(parkDocument, cancellationToken);
        Task<string?> founderNameTask = this.GetFounderNameAsync(parkDocument.FounderId, cancellationToken);
        Task<string?> operatorNameTask = this.GetOperatorNameAsync(parkDocument.OperatorId, cancellationToken);
        Task<RatingSummaryResult> ratingTask = this.GetRatingSummaryAsync(parkDocument.Id, cancellationToken);

        await Task.WhenAll(countsTask, zoneCountTask, mainImageTask, founderNameTask, operatorNameTask, ratingTask);

        IReadOnlyDictionary<ParkItemCategory, int> countsByCategory = await countsTask;
        int totalItems = countsByCategory.Values.Sum();

        return new ParkDetailSummaryResult
        {
            Park = parkDocument.ToDomain(),
            MainImage = await mainImageTask,
            FounderName = await founderNameTask,
            OperatorName = await operatorNameTask,
            Rating = await ratingTask,
            Stats = new ParkDetailSummaryStatsResult
            {
                TotalItems = totalItems,
                ZoneCount = await zoneCountTask,
                AttractionCount = GetCount(countsByCategory, ParkItemCategory.Attraction),
                RestaurantCount = GetCount(countsByCategory, ParkItemCategory.Restaurant),
                ShowCount = GetCount(countsByCategory, ParkItemCategory.Show),
                ShopCount = GetCount(countsByCategory, ParkItemCategory.Shop),
                HotelCount = GetCount(countsByCategory, ParkItemCategory.Hotel),
                CountsByCategory = countsByCategory,
            },
        };
    }

    private async Task<RatingSummaryResult> GetRatingSummaryAsync(string parkId, CancellationToken cancellationToken)
    {
        RatingAggregateDocument? aggregate = await this.ratingAggregatesCollection.Find(
                Builders<RatingAggregateDocument>.Filter.Eq(document => document.TargetType, RatingTargetType.Park)
                & Builders<RatingAggregateDocument>.Filter.Eq(document => document.TargetId, parkId))
            .FirstOrDefaultAsync(cancellationToken);

        if (aggregate is null)
        {
            return new RatingSummaryResult(RatingTargetType.Park, parkId, 0, 0d, RatingScoreCalculator.PriorMean);
        }

        return new RatingSummaryResult(
            aggregate.TargetType,
            aggregate.TargetId,
            aggregate.RatingCount,
            aggregate.AverageRating,
            aggregate.BayesianScore);
    }

    private async Task<IReadOnlyDictionary<ParkItemCategory, int>> GetCountsByCategoryAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId)
            & Builders<ParkItemDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant)
            & BuildClosedFilter(closedFilter);
        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<BsonDocument> aggregationResults = await this.parkItemsCollection.Aggregate()
            .Match(filter)
            .Group(new BsonDocument
            {
                { "_id", "$category" },
                { "count", new BsonDocument("$sum", 1) },
            })
            .ToListAsync(cancellationToken);

        Dictionary<ParkItemCategory, int> counts = new Dictionary<ParkItemCategory, int>();
        foreach (BsonDocument aggregationResult in aggregationResults)
        {
            BsonValue categoryValue = aggregationResult.GetValue("_id", BsonValue.Create(string.Empty));
            string categoryText = categoryValue.IsString ? categoryValue.AsString : categoryValue.ToString() ?? string.Empty;
            if (!Enum.TryParse(categoryText, true, out ParkItemCategory category))
            {
                continue;
            }

            BsonValue countValue = aggregationResult.GetValue("count", BsonValue.Create(0));
            counts[category] = countValue.IsNumeric ? countValue.ToInt32() : 0;
        }

        return counts;
    }

    private async Task<int> GetZoneCountAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkZoneDocument> filter = Builders<ParkZoneDocument>.Filter.Eq(document => document.ParkId, parkId);
        if (!includeHidden)
        {
            filter &= Builders<ParkZoneDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        long count = await this.parkZonesCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > int.MaxValue ? int.MaxValue : checked((int)count);
    }

    private async Task<Image?> GetMainImageAsync(ParkDocument parkDocument, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> currentParkImageFilter = Builders<ImageDocument>.Filter.Eq(document => document.OwnerType, ImageOwnerType.Park)
            & Builders<ImageDocument>.Filter.Eq(document => document.OwnerId, parkDocument.Id)
            & Builders<ImageDocument>.Filter.Eq(document => document.Category, ImageCategory.Park)
            & Builders<ImageDocument>.Filter.Eq(document => document.IsCurrent, true)
            & Builders<ImageDocument>.Filter.Eq(document => document.IsPublished, true);

        ImageDocument? currentParkImage = await this.imagesCollection.Find(currentParkImageFilter).FirstOrDefaultAsync(cancellationToken);
        if (currentParkImage is not null)
        {
            return currentParkImage.ToDomain();
        }

        if (string.IsNullOrWhiteSpace(parkDocument.CurrentLogoImageId))
        {
            return null;
        }

        FilterDefinition<ImageDocument> logoFilter = Builders<ImageDocument>.Filter.Eq(document => document.Id, parkDocument.CurrentLogoImageId.Trim())
            & Builders<ImageDocument>.Filter.Eq(document => document.IsPublished, true);

        ImageDocument? logoImage = await this.imagesCollection.Find(logoFilter).FirstOrDefaultAsync(cancellationToken);
        return logoImage?.ToDomain();
    }

    private async Task<string?> GetFounderNameAsync(string? founderId, CancellationToken cancellationToken)
    {
        string? normalizedFounderId = NormalizeOptionalString(founderId);
        if (normalizedFounderId is null)
        {
            return null;
        }

        string? name = await this.parkFoundersCollection.Find(founder => founder.Id == normalizedFounderId)
            .Project(founder => founder.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return NormalizeOptionalString(name);
    }

    private async Task<string?> GetOperatorNameAsync(string? operatorId, CancellationToken cancellationToken)
    {
        string? normalizedOperatorId = NormalizeOptionalString(operatorId);
        if (normalizedOperatorId is null)
        {
            return null;
        }

        string? name = await this.parkOperatorsCollection.Find(parkOperator => parkOperator.Id == normalizedOperatorId)
            .Project(parkOperator => parkOperator.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return NormalizeOptionalString(name);
    }

    private static int GetCount(IReadOnlyDictionary<ParkItemCategory, int> counts, ParkItemCategory category)
    {
        return counts.TryGetValue(category, out int count) ? count : 0;
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static FilterDefinition<ParkItemDocument> BuildClosedFilter(ClosedEntityFilter closedFilter)
    {
        FilterDefinition<ParkItemDocument> closedFilterDefinition = Builders<ParkItemDocument>.Filter.Regex(
            "attractionDetails.status",
            new BsonRegularExpression("^(closed\\s*definitively|closed-definitively|closed_definitively|closeddefinitively|permanently\\s*closed|permanently-closed|permanently_closed|permanentlyclosed|definitively\\s*closed|definitively-closed|definitively_closed|definitivelyclosed|ferme\\s*definitivement|fermedefinitivement)$", "i"));

        return closedFilter switch
        {
            ClosedEntityFilter.All => Builders<ParkItemDocument>.Filter.Empty,
            ClosedEntityFilter.ClosedOnly => closedFilterDefinition,
            _ => Builders<ParkItemDocument>.Filter.Not(closedFilterDefinition),
        };
    }
}
