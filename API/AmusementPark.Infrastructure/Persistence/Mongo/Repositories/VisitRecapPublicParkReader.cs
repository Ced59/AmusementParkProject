using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class VisitRecapPublicParkReader : IVisitRecapPublicParkReader
{
    private readonly IMongoCollection<ParkDocument> collection;

    public VisitRecapPublicParkReader(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
    }

    public async Task<string?> GetVisibleNameAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        if (normalizedParkId.Length == 0)
        {
            return null;
        }

        IReadOnlyDictionary<string, string> names = await this.GetVisibleNamesAsync(
            new[] { normalizedParkId },
            cancellationToken);
        return names.TryGetValue(normalizedParkId, out string? name) ? name : null;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetVisibleNamesAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkIds);
        string[] normalizedParkIds = parkIds
            .Select(static value => value?.Trim() ?? string.Empty)
            .Where(static value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedParkIds.Length == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        FilterDefinitionBuilder<ParkDocument> filters = Builders<ParkDocument>.Filter;
        List<ParkDocument> parks = await this.collection
            .Find(filters.In(static value => value.Id, normalizedParkIds)
                & filters.Eq(static value => value.IsVisible, true))
            .Project<ParkDocument>(Builders<ParkDocument>.Projection
                .Include(static value => value.Id)
                .Include(static value => value.Name))
            .ToListAsync(cancellationToken);
        return parks
            .Select(static park => new
            {
                park.Id,
                Name = park.Name?.Trim() ?? string.Empty,
            })
            .Where(static park => park.Id.Length > 0 && park.Name.Length > 0)
            .GroupBy(static park => park.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().Name,
                StringComparer.Ordinal);
    }
}
