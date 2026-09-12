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

    public async Task<IReadOnlyCollection<ParkPricingEntity>> GetByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkIds);

        List<string> normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalizedParkIds.Count == 0)
        {
            return Array.Empty<ParkPricingEntity>();
        }

        FilterDefinition<ParkPricingDocument> filter = Builders<ParkPricingDocument>.Filter.In(
            static document => document.ParkId,
            normalizedParkIds);
        List<ParkPricingDocument> documents = await this.collection
            .Find(filter)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<ParkPricingEntity>> GetPublicTextByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkIds);

        List<string> normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalizedParkIds.Count == 0)
        {
            return Array.Empty<ParkPricingEntity>();
        }

        FilterDefinition<ParkPricingDocument> filter = Builders<ParkPricingDocument>.Filter.In(
            static document => document.ParkId,
            normalizedParkIds);
        ProjectionDefinition<ParkPricingDocument> projection = Builders<ParkPricingDocument>.Projection
            .Include(static document => document.ParkId)
            .Include(static document => document.Notes)
            .Include("admissionOffers.labels")
            .Include("admissionOffers.conditions")
            .Include("admissionOffers.validFrom")
            .Include("admissionOffers.validTo")
            .Include("annualPasses.names")
            .Include("annualPasses.conditions")
            .Include("annualPasses.validFrom")
            .Include("annualPasses.validTo")
            .Include("parkingOffers.labels")
            .Include("parkingOffers.conditions")
            .Include("parkingOffers.validFrom")
            .Include("parkingOffers.validTo")
            .Include("creditOffers.labels")
            .Include("creditOffers.conditions")
            .Include("creditOffers.validFrom")
            .Include("creditOffers.validTo")
            .Include("historicalSnapshots.year")
            .Include("historicalSnapshots.notes")
            .Include("historicalSnapshots.admissionOffers.labels")
            .Include("historicalSnapshots.admissionOffers.conditions")
            .Include("historicalSnapshots.annualPasses.names")
            .Include("historicalSnapshots.annualPasses.conditions")
            .Include("historicalSnapshots.parkingOffers.labels")
            .Include("historicalSnapshots.parkingOffers.conditions")
            .Include("historicalSnapshots.creditOffers.labels")
            .Include("historicalSnapshots.creditOffers.conditions");
        List<ParkPricingDocument> documents = await this.collection
            .Find(filter)
            .Project<ParkPricingDocument>(projection)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
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

    public async Task<bool> DeleteByParkIdAsync(string parkId, CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(
            item => item.ParkId == parkId,
            cancellationToken);
        return result.DeletedCount > 0;
    }
}
