using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des zones.
/// </summary>
public sealed class ParkZoneRepository : IParkZoneRepository
{
    private readonly IMongoCollection<ParkZoneDocument> zonesCollection;
    private readonly IMongoCollection<ParkItemDocument> itemsCollection;

    public ParkZoneRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.zonesCollection = database.GetCollection<ParkZoneDocument>(settings.ParkZonesCollectionName);
        this.itemsCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
    }

    public async Task<IReadOnlyCollection<ParkZone>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<ParkZoneDocument> documents = await this.zonesCollection.Find(Builders<ParkZoneDocument>.Filter.Empty)
            .SortBy(document => document.ParkId)
            .ThenBy(document => document.SortOrder)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<ParkZone>> GetByParkIdAsync(string parkId, CancellationToken cancellationToken)
    {
        List<ParkZoneDocument> documents = await this.zonesCollection.Find(document => document.ParkId == parkId)
            .SortBy(document => document.SortOrder)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<ParkZone?> GetByIdAsync(string zoneId, CancellationToken cancellationToken)
    {
        ParkZoneDocument? document = await this.zonesCollection.Find(document => document.Id == zoneId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ParkZone> CreateAsync(ParkZone zone, CancellationToken cancellationToken)
    {
        ParkZoneDocument document = zone.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;

        await this.zonesCollection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<ParkZone?> UpdateAsync(string zoneId, ParkZone zone, CancellationToken cancellationToken)
    {
        ParkZoneDocument document = zone.ToDocument();
        document.Id = zoneId;
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.zonesCollection.ReplaceOneAsync(
            existing => existing.Id == zoneId,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return document.ToDomain();
    }

    public async Task<bool> DeleteAsync(string zoneId, CancellationToken cancellationToken)
    {
        DeleteResult deleteZoneResult = await this.zonesCollection.DeleteOneAsync(
            document => document.Id == zoneId,
            cancellationToken: cancellationToken);

        await this.itemsCollection.UpdateManyAsync(
            document => document.ZoneId == zoneId,
            Builders<ParkItemDocument>.Update
                .Set(document => document.ZoneId, null)
                .Set(document => document.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return deleteZoneResult.DeletedCount > 0;
    }

    public async Task<ParkExplorerResult> GetExplorerAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkZoneDocument> zoneFilter = Builders<ParkZoneDocument>.Filter.Eq(document => document.ParkId, parkId);
        FilterDefinition<ParkItemDocument> itemFilter = Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId);

        if (!includeHidden)
        {
            zoneFilter &= Builders<ParkZoneDocument>.Filter.Eq(document => document.IsVisible, true);
            itemFilter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<ParkZoneDocument> zoneDocuments = await this.zonesCollection.Find(zoneFilter)
            .SortBy(document => document.SortOrder)
            .ThenBy(document => document.Name)
            .ToListAsync(cancellationToken);

        List<ParkItemDocument> itemDocuments = await this.itemsCollection.Find(itemFilter)
            .SortBy(document => document.Category)
            .ThenBy(document => document.Type)
            .ThenBy(document => document.Name)
            .ToListAsync(cancellationToken);

        return new ParkExplorerResult
        {
            ParkId = parkId,
            Zones = zoneDocuments.Select(document => document.ToDomain()).ToList(),
            Items = itemDocuments.Select(document => document.ToDomain()).ToList(),
        };
    }
}
