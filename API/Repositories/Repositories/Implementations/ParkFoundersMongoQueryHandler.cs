using System.Collections.Generic;
using System.Threading.Tasks;
using Entities.Model.Parks;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations;

public class ParkFoundersMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
    : IParkFoundersQueryHandler
{
    private readonly IMongoCollection<ParkFounder> foundersCollection = database.GetCollection<ParkFounder>(settings.ParkFoundersCollectionName);

    public async Task<IEnumerable<ParkFounder>> GetAllAsync()
    {
        return await foundersCollection.Find(Builders<ParkFounder>.Filter.Empty)
            .SortBy(founder => founder.Name)
            .ToListAsync();
    }

    public async Task<ParkFounder?> GetByIdAsync(string id)
    {
        return await foundersCollection.Find(founder => founder.Id == id).FirstOrDefaultAsync();
    }

    public async Task<ParkFounder?> CreateAsync(ParkFounder founder)
    {
        try
        {
            await foundersCollection.InsertOneAsync(founder);
            return founder;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ParkFounder?> UpdateAsync(ParkFounder founder)
    {
        if (string.IsNullOrWhiteSpace(founder.Id))
        {
            return null;
        }

        ReplaceOneResult result = await foundersCollection.ReplaceOneAsync(
            item => item.Id == founder.Id,
            founder);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return founder;
    }
}