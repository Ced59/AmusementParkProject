using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Lecture Mongo minimale des noms de parcs.
/// </summary>
public sealed class ParkNameReadRepository : IParkNameReadRepository
{
    private readonly IMongoCollection<ParkDocument> collection;

    public ParkNameReadRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetNamesByIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalizedParkIds.Count == 0)
        {
            return new Dictionary<string, string?>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.In(
            document => document.Id,
            normalizedParkIds);
        List<ParkDocument> documents = await this.collection
            .Find(filter)
            .Project<ParkDocument>(BuildNameProjection())
            .ToListAsync(cancellationToken);
        return documents
            .GroupBy(static document => document.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().Name,
                StringComparer.Ordinal);
    }

    internal static ProjectionDefinition<ParkDocument> BuildNameProjection()
    {
        return Builders<ParkDocument>.Projection
            .Include(document => document.Id)
            .Include(document => document.Name);
    }
}
