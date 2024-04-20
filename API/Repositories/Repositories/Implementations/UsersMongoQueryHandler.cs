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
        private readonly IMongoCollection<UserInDb> _usersCollection;

        public UsersMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            _usersCollection = database.GetCollection<UserInDb>(settings.UsersCollectionName);
        }

        public async Task<bool> ExistsByEmailAsync(string? email)
        {
            var count = await _usersCollection.CountDocumentsAsync(user => user.Email == email);
            return count > 0;
        }

        public async Task<UserInDb> GetUserByIdAsync(string id)
        {
            return await _usersCollection.Find(user => user.Id == id).FirstOrDefaultAsync();
        }

        public async Task<UserInDb> GetUserByEmailAsync(string? email)
        {
            return await _usersCollection.Find(user => user.Email == email).FirstOrDefaultAsync();
        }


        public async Task<IEnumerable<UserInDb>> GetAllUsersAsync()
        {
            return await _usersCollection.Find(user => true).ToListAsync();
        }

        public async Task<UserInDb> CreateUserAsync(UserInDb user)
        {
            await _usersCollection.InsertOneAsync(user);
            return user;
        }

        public async Task UpdateUserAsync(UserInDb user)
        {
            await _usersCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
        }

        public async Task DeleteUserAsync(string id)
        {
            await _usersCollection.DeleteOneAsync(user => user.Id == id);
        }
    }
}
