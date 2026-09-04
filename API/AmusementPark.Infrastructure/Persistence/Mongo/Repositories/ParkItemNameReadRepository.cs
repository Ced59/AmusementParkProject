using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Lecture Mongo minimale des noms des éléments de parc.
/// </summary>
public sealed class ParkItemNameReadRepository : IParkItemNameReadRepository
{
    private readonly IMongoCollection<ParkItemDocument> collection;

    public ParkItemNameReadRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkItemDocument>(
            settings.ParkItemsCollectionName);
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetNamesByIdsAsync(
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        List<string> normalizedParkItemIds = parkItemIds
            .Where(static parkItemId => !string.IsNullOrWhiteSpace(parkItemId))
            .Select(static parkItemId => parkItemId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalizedParkItemIds.Count == 0)
        {
            return new Dictionary<string, string?>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.In(
            document => document.Id,
            normalizedParkItemIds);
        List<ParkItemDocument> documents = await this.collection
            .Find(filter)
            .Project<ParkItemDocument>(BuildNameProjection())
            .ToListAsync(cancellationToken);
        return documents
            .GroupBy(static document => document.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => (string?)group.First().Name,
                StringComparer.Ordinal);
    }

    internal static ProjectionDefinition<ParkItemDocument> BuildNameProjection()
    {
        return Builders<ParkItemDocument>.Projection
            .Include(document => document.Id)
            .Include(document => document.Name);
    }
}
