using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using MongoDB.Driver;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class StandaloneAttractionPricingRepository : IStandaloneAttractionPricingRepository
{
    private readonly ParkPricingRepository repository;

    public StandaloneAttractionPricingRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.repository = new ParkPricingRepository(
            database,
            settings.StandaloneAttractionPricingCollectionName);
    }

    public Task<ParkPricingEntity?> GetByStandaloneAttractionIdAsync(
        string standaloneAttractionId,
        CancellationToken cancellationToken)
    {
        return this.repository.GetByParkIdAsync(standaloneAttractionId, cancellationToken);
    }

    public Task<ParkPricingEntity> UpsertAsync(
        ParkPricingEntity pricing,
        CancellationToken cancellationToken)
    {
        return this.repository.UpsertAsync(pricing, cancellationToken);
    }
}
