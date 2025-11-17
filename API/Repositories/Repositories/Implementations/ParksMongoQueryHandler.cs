using Entities.Model.Parks;
using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations;

public class ParksMongoQueryHandler : IParksQueryHandler
{
    private readonly IMongoCollection<Park> parksCollection;

    public ParksMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
    {
        parksCollection = database.GetCollection<Park>(settings.ParksCollectionName);
    }

    public async Task<Park?> GetParkByIdAsync(string id)
    {
        return await parksCollection.Find(park => park.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Park>> GetParksPaginatedAsync(int page, int pageSize)
    {
        return await parksCollection.Find(_ => true)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<long> GetTotalParksCountAsync()
    {
        return await parksCollection.CountDocumentsAsync(_ => true);
    }

    public async Task<Park?> CreateParkAsync(Park park)
    {
        try
        {
            await parksCollection.InsertOneAsync(park);
            return park;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<Park>> GetParksByLocationAsync(double latitude, double longitude, double maxDistanceInMeters)
    {
        BsonDocument filter = new("Location", new BsonDocument("$nearSphere", new BsonDocument
        {
            { "$geometry", new BsonDocument
                {
                    { "type", "Point" },
                    { "coordinates", new BsonArray { longitude, latitude } }
                }
            },
            { "$maxDistance", maxDistanceInMeters }
        }));

        List<Park>? parks = await parksCollection.Find(filter).ToListAsync();
        return parks;
    }

    public async Task<long> GetTotalParksCountByNameAsync(string name)
    {
        // filtre : name contient "name", insensible à la casse
        FilterDefinition<Park>? filter = Builders<Park>.Filter.Regex(
            p => p.Name,
            new BsonRegularExpression(name, "i"));

        return await parksCollection.CountDocumentsAsync(filter);
    }

    public async Task<IEnumerable<Park>> GetParksByNamePaginatedAsync(string name, int page, int pageSize)
    {
        FilterDefinition<Park>? filter = Builders<Park>.Filter.Regex(
            p => p.Name,
            new BsonRegularExpression(name, "i"));

        return await parksCollection
            .Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<Park?> UpdateParkVisibilityAsync(string id, bool isVisible)
    {
        var filter = Builders<Park>.Filter.Eq(p => p.Id, id);

        var update = Builders<Park>.Update
            .Set(p => p.IsVisible, isVisible)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Park>
        {
            ReturnDocument = ReturnDocument.After // on récupère la version après update
        };

        Park? updated = await parksCollection.FindOneAndUpdateAsync(filter, update, options);

        return updated;
    }
}