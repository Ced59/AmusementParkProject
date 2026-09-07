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

        FilterDefinitionBuilder<ParkDocument> filters = Builders<ParkDocument>.Filter;
        ParkDocument? park = await this.collection
            .Find(filters.Eq(static value => value.Id, normalizedParkId)
                & filters.Eq(static value => value.IsVisible, true))
            .Project<ParkDocument>(Builders<ParkDocument>.Projection
                .Include(static value => value.Id)
                .Include(static value => value.Name))
            .FirstOrDefaultAsync(cancellationToken);
        string name = park?.Name?.Trim() ?? string.Empty;
        return name.Length == 0 ? null : name;
    }
}
