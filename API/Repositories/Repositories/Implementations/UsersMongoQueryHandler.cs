using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.Model.Users;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class UsersMongoQueryHandler : IUserQueryHandler
    {
        private readonly IMongoCollection<User> _usersCollection;

        public UsersMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            _usersCollection = database.GetCollection<User>(settings.UsersCollectionName);
        }

        public async Task<bool> ExistsByEmailAsync(string? email)
        {
            var count = await _usersCollection.CountDocumentsAsync(user => user.Email == email);
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
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            var options = new FindOneAndReplaceOptions<User>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };

            try
            {
                return await _usersCollection.FindOneAndReplaceAsync(filter, user, options);
            }
            catch (Exception ex)
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
            var now = DateTime.UtcNow; 

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.LastLogin, now)
                .Set(u => u.LastActivity, now);


            await _usersCollection.UpdateOneAsync(filter, update);

        }

        public async Task UpdateLastActivityAsync(string userId)
        {
            var now = DateTime.UtcNow;

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.LastActivity, now);


            await _usersCollection.UpdateOneAsync(filter, update);

        }
    }
}
