using System.Collections.Generic;
using System.Threading.Tasks;
using Entities.Model.Parks;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations;

public class ParkOperatorsMongoQueryHandler : IParkOperatorsQueryHandler
{
    private readonly IMongoCollection<ParkOperator> operatorsCollection;

    public ParkOperatorsMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
    {
        operatorsCollection = database.GetCollection<ParkOperator>(settings.ParkOperatorsCollectionName);
    }

    public async Task<IEnumerable<ParkOperator>> GetAllAsync()
    {
        return await operatorsCollection.Find(Builders<ParkOperator>.Filter.Empty)
            .SortBy(parkOperator => parkOperator.Name)
            .ToListAsync();
    }

    public async Task<ParkOperator?> GetByIdAsync(string id)
    {
        return await operatorsCollection.Find(parkOperator => parkOperator.Id == id).FirstOrDefaultAsync();
    }

    public async Task<ParkOperator?> CreateAsync(ParkOperator parkOperator)
    {
        try
        {
            await operatorsCollection.InsertOneAsync(parkOperator);
            return parkOperator;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ParkOperator?> UpdateAsync(ParkOperator parkOperator)
    {
        if (string.IsNullOrWhiteSpace(parkOperator.Id))
        {
            return null;
        }

        ReplaceOneResult result = await operatorsCollection.ReplaceOneAsync(
            item => item.Id == parkOperator.Id,
            parkOperator);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return parkOperator;
    }
}