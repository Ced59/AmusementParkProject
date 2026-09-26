using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoricalSubjectPublicationStateReader : IHistoricalSubjectPublicationStateReader
{
    private readonly IMongoCollection<ParkDocument> parks;
    private readonly IMongoCollection<ParkItemDocument> parkItems;
    private readonly IMongoCollection<ParkZoneDocument> parkZones;
    private readonly IMongoCollection<StandaloneAttractionDocument> standaloneAttractions;
    private readonly IMongoCollection<ParkOperatorDocument> parkOperators;
    private readonly IMongoCollection<AttractionManufacturerDocument> attractionManufacturers;

    public HistoricalSubjectPublicationStateReader(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.parks = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItems = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
        this.parkZones = database.GetCollection<ParkZoneDocument>(settings.ParkZonesCollectionName);
        this.standaloneAttractions = database.GetCollection<StandaloneAttractionDocument>(
            settings.StandaloneAttractionsCollectionName);
        this.parkOperators = database.GetCollection<ParkOperatorDocument>(settings.ParkOperatorsCollectionName);
        this.attractionManufacturers = database.GetCollection<AttractionManufacturerDocument>(
            settings.AttractionManufacturersCollectionName);
    }

    public async Task<bool> IsPublicAsync(
        HistoricalSubject subject,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return subject.Type switch
        {
            HistoricalSubjectType.Park => await this.IsParkPublicAsync(subject.Id, cancellationToken),
            HistoricalSubjectType.ParkItem => await this.IsParkItemPublicAsync(subject.Id, cancellationToken),
            HistoricalSubjectType.StandaloneAttraction =>
                await this.IsStandaloneAttractionPublicAsync(subject.Id, cancellationToken),
            HistoricalSubjectType.ParkZone => await this.IsParkZonePublicAsync(subject.Id, cancellationToken),
            HistoricalSubjectType.ParkOperator => await this.IsParkOperatorPublicAsync(subject.Id, cancellationToken),
            HistoricalSubjectType.AttractionManufacturer =>
                await this.IsAttractionManufacturerPublicAsync(subject.Id, cancellationToken),
            _ => false,
        };
    }

    private async Task<bool> IsParkPublicAsync(string parkId, CancellationToken cancellationToken)
    {
        ParkDocument? park = await this.parks
            .Find(item => item.Id == parkId)
            .FirstOrDefaultAsync(cancellationToken);
        return IsParkPublic(park);
    }

    private async Task<bool> IsParkItemPublicAsync(string parkItemId, CancellationToken cancellationToken)
    {
        ParkItemDocument? parkItem = await this.parkItems
            .Find(item => item.Id == parkItemId)
            .FirstOrDefaultAsync(cancellationToken);
        return parkItem is not null
            && parkItem.IsVisible
            && parkItem.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && await this.IsParkPublicAsync(parkItem.ParkId, cancellationToken);
    }

    private async Task<bool> IsStandaloneAttractionPublicAsync(
        string attractionId,
        CancellationToken cancellationToken)
    {
        StandaloneAttractionDocument? attraction = await this.standaloneAttractions
            .Find(item => item.Id == attractionId)
            .FirstOrDefaultAsync(cancellationToken);
        return attraction is not null
            && attraction.IsVisible
            && attraction.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private async Task<bool> IsParkZonePublicAsync(string zoneId, CancellationToken cancellationToken)
    {
        ParkZoneDocument? zone = await this.parkZones
            .Find(item => item.Id == zoneId)
            .FirstOrDefaultAsync(cancellationToken);
        return zone is not null
            && zone.IsVisible
            && await this.IsParkPublicAsync(zone.ParkId, cancellationToken);
    }

    private async Task<bool> IsParkOperatorPublicAsync(string operatorId, CancellationToken cancellationToken)
    {
        ParkOperatorDocument? parkOperator = await this.parkOperators
            .Find(item => item.Id == operatorId)
            .FirstOrDefaultAsync(cancellationToken);
        return parkOperator is not null
            && parkOperator.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private async Task<bool> IsAttractionManufacturerPublicAsync(
        string manufacturerId,
        CancellationToken cancellationToken)
    {
        AttractionManufacturerDocument? manufacturer = await this.attractionManufacturers
            .Find(item => item.Id == manufacturerId)
            .FirstOrDefaultAsync(cancellationToken);
        return manufacturer is not null
            && manufacturer.IsVisible
            && manufacturer.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static bool IsParkPublic(ParkDocument? park)
    {
        return park is not null
            && park.IsVisible
            && park.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && park.Status.CanAppearInPublicDiscovery();
    }
}
