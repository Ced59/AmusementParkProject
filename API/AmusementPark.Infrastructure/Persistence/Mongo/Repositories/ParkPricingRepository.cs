using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkPricingRepository : IParkPricingRepository
{
    private readonly IMongoCollection<ParkPricingDocument> collection;

    public ParkPricingRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkPricingDocument>(settings.ParkPricingCollectionName);
    }

    public async Task<ParkPricingEntity?> GetByParkIdAsync(string parkId, CancellationToken cancellationToken)
    {
        ParkPricingDocument? document = await this.collection
            .Find(item => item.ParkId == parkId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ParkPricingEntity> UpsertAsync(ParkPricingEntity pricing, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        ParkPricingDocument? existing = await this.collection
            .Find(item => item.ParkId == pricing.ParkId)
            .Project(static item => new ParkPricingDocument
            {
                Id = item.Id,
                CreatedAt = item.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        ParkPricingDocument document = pricing.ToDocument();
        document.Id = existing?.Id ?? Guid.NewGuid().ToString("N");
        document.CreatedAt = existing?.CreatedAt ?? now;
        document.UpdatedAt = now;

        await this.collection.ReplaceOneAsync(
            item => item.ParkId == document.ParkId,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        return document.ToDomain();
    }
}
