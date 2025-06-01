using Entities.Model.Parks;
using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations;

public class ParksQueryHandler : IParksQueryHandler
{
    private readonly IMongoCollection<Park> _parksCollection;

    public ParksQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
    {
        _parksCollection = database.GetCollection<Park>(settings.ParksCollectionName);
    }

    public async Task<Park?> GetParkByIdAsync(string id)
    {
        return await _parksCollection.Find(park => park.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Park>> GetParksPaginatedAsync(int page, int pageSize)
    {
        return await _parksCollection.Find(_ => true)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<long> GetTotalParksCountAsync()
    {
        return await _parksCollection.CountDocumentsAsync(_ => true);
    }

    public async Task<Park?> CreateParkAsync(Park park)
    {
        try
        {
            await _parksCollection.InsertOneAsync(park);
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

        List<Park>? parks = await _parksCollection.Find(filter).ToListAsync();
        return parks;
    }

}