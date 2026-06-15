using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
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

    public async Task<ParkMapItemsResult?> GetAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
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
        Task<List<ParkItemDocument>> itemsTask = this.GetItemsAsync(parkId, includeHidden, cancellationToken);

        await Task.WhenAll(zonesTask, itemsTask);

        List<ParkZoneDocument> zoneDocuments = await zonesTask;
        List<ParkItemDocument> itemDocuments = await itemsTask;

        return new ParkMapItemsResult
        {
            Park = parkDocument.ToDomain(),
            Zones = zoneDocuments.Select(static document => new ParkMapZoneResult
            {
                Id = document.Id,
                Name = document.Name,
                SortOrder = document.SortOrder,
            }).ToList(),
            Items = itemDocuments
                .Where(static document => HasValidCoordinates(document.Latitude, document.Longitude))
                .Select(static document => new ParkMapItemResult
                {
                    Id = document.Id,
                    Name = document.Name,
                    Category = document.Category,
                    Type = document.Type,
                    Subtype = document.Subtype,
                    ZoneId = string.IsNullOrWhiteSpace(document.ZoneId) ? null : document.ZoneId,
                    Latitude = document.Latitude!.Value,
                    Longitude = document.Longitude!.Value,
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

    private Task<List<ParkItemDocument>> GetItemsAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId)
            & Builders<ParkItemDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant)
            & Builders<ParkItemDocument>.Filter.Exists(document => document.Latitude, true)
            & Builders<ParkItemDocument>.Filter.Exists(document => document.Longitude, true)
            & Builders<ParkItemDocument>.Filter.Gte(document => document.Latitude, -90d)
            & Builders<ParkItemDocument>.Filter.Lte(document => document.Latitude, 90d)
            & Builders<ParkItemDocument>.Filter.Gte(document => document.Longitude, -180d)
            & Builders<ParkItemDocument>.Filter.Lte(document => document.Longitude, 180d)
            & Builders<ParkItemDocument>.Filter.Or(
                Builders<ParkItemDocument>.Filter.Ne(document => document.Latitude, 0d),
                Builders<ParkItemDocument>.Filter.Ne(document => document.Longitude, 0d));

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
}
