using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo spécialisé pour les marqueurs publics de carte d'un parc.
/// </summary>
public sealed class ParkMapItemsReadRepository : IParkMapItemsReadRepository
{
    private readonly IMongoCollection<ParkDocument> parksCollection;
    private readonly IMongoCollection<ParkItemDocument> parkItemsCollection;
    private readonly IMongoCollection<ParkZoneDocument> parkZonesCollection;

    public ParkMapItemsReadRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.parksCollection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItemsCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
        this.parkZonesCollection = database.GetCollection<ParkZoneDocument>(settings.ParkZonesCollectionName);
    }

    public async Task<ParkMapItemsResult?> GetAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
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

        Task<List<ParkZoneDocument>> zonesTask = this.GetZonesAsync(parkId, includeHidden, cancellationToken);
        Task<List<ParkItemDocument>> itemsTask = this.GetItemsAsync(parkId, includeHidden, closedFilter, cancellationToken);

        await Task.WhenAll(zonesTask, itemsTask);

        List<ParkZoneDocument> zoneDocuments = await zonesTask;
        List<ParkItemDocument> itemDocuments = await itemsTask;
        List<ParkItemDocument> locatedDocuments = itemDocuments
            .Where(static document => HasValidCoordinates(document.Latitude, document.Longitude))
            .ToList();
        List<ParkItemDocument> unlocatedDocuments = itemDocuments
            .Where(static document => !HasValidCoordinates(document.Latitude, document.Longitude))
            .ToList();

        return new ParkMapItemsResult
        {
            Park = parkDocument.ToDomain(),
            Zones = zoneDocuments.Select(static document => new ParkMapZoneResult
            {
                Id = document.Id,
                Name = document.Name,
                SortOrder = document.SortOrder,
            }).ToList(),
            Items = locatedDocuments
                .Select(static document => new ParkMapItemResult
                {
                    Id = document.Id,
                    Name = document.Name,
                    Category = document.Category,
                    Type = document.Type,
                    Subtype = document.Subtype,
                    ZoneId = string.IsNullOrWhiteSpace(document.ZoneId) ? null : document.ZoneId,
                    Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
                    AttractionDetails = ToMapAttractionDetails(document.AttractionDetails),
                    Latitude = document.Latitude!.Value,
                    Longitude = document.Longitude!.Value,
                })
                .ToList(),
            UnlocatedItems = unlocatedDocuments
                .Select(static document => new ParkMapUnlocatedItemResult
                {
                    Id = document.Id,
                    Name = document.Name,
                    Category = document.Category,
                    Type = document.Type,
                    Subtype = document.Subtype,
                    ZoneId = string.IsNullOrWhiteSpace(document.ZoneId) ? null : document.ZoneId,
                    Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
                    AttractionDetails = ToMapAttractionDetails(document.AttractionDetails),
                })
                .ToList(),
        };
    }

    private Task<List<ParkZoneDocument>> GetZonesAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkZoneDocument> filter = Builders<ParkZoneDocument>.Filter.Eq(document => document.ParkId, parkId);
        if (!includeHidden)
        {
            filter &= Builders<ParkZoneDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        return this.parkZonesCollection.Find(filter)
            .Project(static document => new ParkZoneDocument
            {
                Id = document.Id,
                ParkId = document.ParkId,
                Name = document.Name,
                Names = document.Names,
                IsVisible = document.IsVisible,
                SortOrder = document.SortOrder,
            })
            .SortBy(static document => document.SortOrder)
            .ThenBy(static document => document.Name)
            .ToListAsync(cancellationToken);
    }

    private Task<List<ParkItemDocument>> GetItemsAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId)
            & Builders<ParkItemDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant)
            & BuildClosedFilter(closedFilter);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        return this.parkItemsCollection.Find(filter)
            .Project(static document => new ParkItemDocument
            {
                Id = document.Id,
                ParkId = document.ParkId,
                ZoneId = document.ZoneId,
                Name = document.Name,
                Category = document.Category,
                Type = document.Type,
                Subtype = document.Subtype,
                Descriptions = document.Descriptions,
                AttractionDetails = document.AttractionDetails,
                Latitude = document.Latitude,
                Longitude = document.Longitude,
                IsVisible = document.IsVisible,
                AdminReviewStatus = document.AdminReviewStatus,
            })
            .SortBy(static document => document.Category)
            .ThenBy(static document => document.Name)
            .ThenBy(static document => document.Id)
            .ToListAsync(cancellationToken);
    }

    private static bool HasValidCoordinates(double? latitude, double? longitude)
    {
        return latitude.HasValue
            && longitude.HasValue
            && double.IsFinite(latitude.Value)
            && double.IsFinite(longitude.Value)
            && Math.Abs(latitude.Value) <= 90d
            && Math.Abs(longitude.Value) <= 180d
            && !(Math.Abs(latitude.Value) < double.Epsilon && Math.Abs(longitude.Value) < double.Epsilon);
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

    private static ParkMapAttractionDetailsResult? ToMapAttractionDetails(AttractionDetailsDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        return new ParkMapAttractionDetailsResult
        {
            ManufacturerId = string.IsNullOrWhiteSpace(document.ManufacturerId) ? null : document.ManufacturerId,
            Model = string.IsNullOrWhiteSpace(document.Model) ? null : document.Model,
            Status = string.IsNullOrWhiteSpace(document.Status) ? null : document.Status,
        };
    }
}
