using Common.Users;
using Entities.Model.Users;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations;

public class UsersMongoQueryHandler : IUserQueryHandler
{
    private readonly IMongoCollection<User> _usersCollection;

    public UsersMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
    {
        _usersCollection = database.GetCollection<User>(settings.UsersCollectionName);
    }

    public async Task<bool> ExistsByEmailAsync(string? email)
    {
        long count = await _usersCollection.CountDocumentsAsync(user => user.Email == email);
        return count > 0;
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _usersCollection.Find(user => user.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string? email)
    {
        return await _usersCollection.Find(user => user.Email == email).FirstOrDefaultAsync();
    }


    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _usersCollection.Find(user => true).ToListAsync();
    }

    public async Task<User?> CreateUserAsync(User user)
    {
        try
        {
            await _usersCollection.InsertOneAsync(user);
            return user;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<User?> UpdateUserAsync(User user)
    {
        FilterDefinition<User>? filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
        FindOneAndReplaceOptions<User> options = new FindOneAndReplaceOptions<User>
        {
            ReturnDocument = ReturnDocument.After,
            IsUpsert = false
        };

        try
        {
            return await _usersCollection.FindOneAndReplaceAsync(filter, user, options);
        }
        catch (Exception)
        {
            return null;
        }
    }


    public async Task DeleteUserAsync(string id)
    {
        await _usersCollection.DeleteOneAsync(user => user.Id == id);
    }

    public async Task UpdateLastLoginAndActivityAsync(string userId)
    {
        DateTime now = DateTime.UtcNow;

        FilterDefinition<User>? filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        UpdateDefinition<User>? update = Builders<User>.Update
            .Set(u => u.LastLogin, now)
            .Set(u => u.LastActivity, now);


        await _usersCollection.UpdateOneAsync(filter, update);
    }

    public async Task UpdateLastActivityAsync(string userId)
    {
        DateTime now = DateTime.UtcNow;

        FilterDefinition<User>? filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        UpdateDefinition<User>? update = Builders<User>.Update
            .Set(u => u.LastActivity, now);


        await _usersCollection.UpdateOneAsync(filter, update);
    }

    public async Task<User?> AssignRoleAsync(string userId, Role role)
    {
        FilterDefinition<User>? filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        UpdateDefinition<User>? update = Builders<User>.Update.AddToSet(u => u.Roles, role);

        FindOneAndUpdateOptions<User> options = new FindOneAndUpdateOptions<User>
        {
            ReturnDocument = ReturnDocument.After
        };

        User? updatedUser = await _usersCollection.FindOneAndUpdateAsync(filter, update, options);

        return updatedUser;
    }

    public async Task<User?> RemoveRoleAsync(string userId, Role role)
    {
        FilterDefinition<User>? filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        UpdateDefinition<User>? update = Builders<User>.Update.Pull(u => u.Roles, role);

        FindOneAndUpdateOptions<User> options = new FindOneAndUpdateOptions<User>
        {
            ReturnDocument = ReturnDocument.After
        };

        User? updatedUser = await _usersCollection.FindOneAndUpdateAsync(filter, update, options);

        return updatedUser;
    }

    public async Task<User?> LockUser(string userId)
    {
        FilterDefinition<User>? filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        UpdateDefinition<User>? update = Builders<User>.Update.Set(u => u.IsBlocked, true);

        FindOneAndUpdateOptions<User> options = new FindOneAndUpdateOptions<User>
        {
            ReturnDocument = ReturnDocument.After
        };

        User? updatedUser = await _usersCollection.FindOneAndUpdateAsync(filter, update, options);

        return updatedUser;
    }

    public async Task<User?> UnlockUser(string userId)
    {
        FilterDefinition<User>? filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        UpdateDefinition<User>? update = Builders<User>.Update.Set(u => u.IsBlocked, false);

        FindOneAndUpdateOptions<User> options = new FindOneAndUpdateOptions<User>
        {
            ReturnDocument = ReturnDocument.After
        };

        User? updatedUser = await _usersCollection.FindOneAndUpdateAsync(filter, update, options);

        return updatedUser;
    }

    public async Task<bool> ChangePassword(string idUser, string newHashedPassword)
    {
        FilterDefinition<User>? filter = Builders<User>.Filter.Eq(u => u.Id, idUser);
        UpdateDefinition<User>? update = Builders<User>.Update.Set(u => u.HashedPassword, newHashedPassword);

        UpdateResult? result = await _usersCollection.UpdateOneAsync(filter, update);

        return result.ModifiedCount == 1;

    }

    public async Task<IEnumerable<User>> GetUsersPaginatedAsync(int page, int pageSize)
    {
        return await _usersCollection.Find(_ => true)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<long> GetTotalUsersCountAsync()
    {
        return await _usersCollection.CountDocumentsAsync(_ => true);
    }
}