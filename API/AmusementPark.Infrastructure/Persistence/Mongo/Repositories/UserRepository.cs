using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des utilisateurs.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IMongoCollection<UserDocument> collection;

    public UserRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<UserDocument>(settings.UsersCollectionName);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        UserDocument? document = await this.collection.Find(document => document.Email == email).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken)
    {
        UserDocument? document = await this.collection.Find(document => document.Id == userId).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<PagedResult<User>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        FilterDefinition<UserDocument> filter = Builders<UserDocument>.Filter.Empty;
        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<UserDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.Email)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken)
    {
        UserDocument document = user.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<User?> UpdateAsync(string userId, User user, CancellationToken cancellationToken)
    {
        UserDocument document = user.ToDocument();
        document.Id = userId;
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            existing => existing.Id == userId,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return document.ToDomain();
    }

    public Task<User?> AssignRoleAsync(string userId, Role role, CancellationToken cancellationToken)
    {
        return this.UpdateRolesAsync(userId, roles =>
        {
            if (!roles.Contains(role))
            {
                roles.Add(role);
            }
        }, cancellationToken);
    }

    public Task<User?> RemoveRoleAsync(string userId, Role role, CancellationToken cancellationToken)
    {
        return this.UpdateRolesAsync(userId, roles =>
        {
            roles.Remove(role);
        }, cancellationToken);
    }

    public Task<User?> LockAsync(string userId, CancellationToken cancellationToken)
    {
        return this.UpdateBooleanAsync(userId, document => document.IsBlocked, true, cancellationToken);
    }

    public Task<User?> UnlockAsync(string userId, CancellationToken cancellationToken)
    {
        return this.UpdateBooleanAsync(userId, document => document.IsBlocked, false, cancellationToken);
    }

    public async Task<User?> ConfirmEmailAsync(string token, CancellationToken cancellationToken)
    {
        FilterDefinition<UserDocument> filter = Builders<UserDocument>.Filter.Eq(document => document.EmailConfirmationTokenHash, token);
        UpdateDefinition<UserDocument> update = Builders<UserDocument>.Update
            .Set(document => document.IsActivated, true)
            .Set(document => document.EmailConfirmationTokenHash, null)
            .Set(document => document.EmailConfirmationTokenExpiresAtUtc, null)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<UserDocument> options = new FindOneAndUpdateOptions<UserDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        UserDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return document?.ToDomain();
    }

    public async Task<bool> ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken)
    {
        UpdateResult result = await this.collection.UpdateOneAsync(
            document => document.Email == email,
            Builders<UserDocument>.Update
                .Set(document => document.EmailConfirmationSentAtUtc, DateTime.UtcNow)
                .Set(document => document.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return result.MatchedCount > 0;
    }

    public async Task<bool> RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        UpdateResult result = await this.collection.UpdateOneAsync(
            document => document.Email == email,
            Builders<UserDocument>.Update
                .Set(document => document.PasswordResetSentAtUtc, DateTime.UtcNow)
                .Set(document => document.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return result.MatchedCount > 0;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPasswordHash, CancellationToken cancellationToken)
    {
        FilterDefinition<UserDocument> filter = Builders<UserDocument>.Filter.Eq(document => document.PasswordResetTokenHash, token);
        UpdateDefinition<UserDocument> update = Builders<UserDocument>.Update
            .Set(document => document.HashedPassword, newPasswordHash)
            .Set(document => document.PasswordResetTokenHash, null)
            .Set(document => document.PasswordResetTokenExpiresAtUtc, null)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        UpdateResult result = await this.collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.MatchedCount > 0;
    }

    public async Task<User?> ChangePasswordAsync(string userId, string newPasswordHash, CancellationToken cancellationToken)
    {
        FilterDefinition<UserDocument> filter = Builders<UserDocument>.Filter.Eq(document => document.Id, userId);
        UpdateDefinition<UserDocument> update = Builders<UserDocument>.Update
            .Set(document => document.HashedPassword, newPasswordHash)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<UserDocument> options = new FindOneAndUpdateOptions<UserDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        UserDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return document?.ToDomain();
    }

    public Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    private async Task<User?> UpdateRolesAsync(string userId, Action<List<Role>> updateRoles, CancellationToken cancellationToken)
    {
        UserDocument? document = await this.collection.Find(document => document.Id == userId).FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        updateRoles(document.Roles);
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            existing => existing.Id == userId,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return document.ToDomain();
    }

    private async Task<User?> UpdateBooleanAsync(string userId, System.Linq.Expressions.Expression<Func<UserDocument, bool>> field, bool value, CancellationToken cancellationToken)
    {
        FilterDefinition<UserDocument> filter = Builders<UserDocument>.Filter.Eq(document => document.Id, userId);
        UpdateDefinition<UserDocument> update = Builders<UserDocument>.Update
            .Set(field, value)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<UserDocument> options = new FindOneAndUpdateOptions<UserDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        UserDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return document?.ToDomain();
    }
}
