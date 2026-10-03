using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Users.Models;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class AccountDeletionOperationRepository
    : IAccountDeletionOperationRepository
{
    private readonly IMongoCollection<AccountDeletionOperationDocument> collection;

    public AccountDeletionOperationRepository(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        this.collection = database.GetCollection<AccountDeletionOperationDocument>(
            settings.AccountDeletionOperationsCollectionName);
    }

    public async Task<AccountDeletionOperation> CreateOrGetAsync(
        string userId,
        DateTime createdAtUtc,
        CancellationToken cancellationToken)
    {
        string userKey = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(userId)));
        UpdateDefinition<AccountDeletionOperationDocument> update =
            Builders<AccountDeletionOperationDocument>.Update
                .SetOnInsert(document => document.Id, Guid.NewGuid().ToString("N"))
                .SetOnInsert(document => document.UserKey, userKey)
                .SetOnInsert(document => document.UserId, userId)
                .SetOnInsert(document => document.CreatedAt, createdAtUtc)
                .SetOnInsert(document => document.UpdatedAt, createdAtUtc);
        FindOneAndUpdateOptions<
            AccountDeletionOperationDocument,
            AccountDeletionOperationDocument> options = new()
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
        };
        FilterDefinition<AccountDeletionOperationDocument> filter =
            Builders<AccountDeletionOperationDocument>.Filter.Eq(
                document => document.UserKey,
                userKey);
        AccountDeletionOperationDocument document;
        try
        {
            document = await this.collection
                .FindOneAndUpdateAsync<AccountDeletionOperationDocument>(
                    filter,
                    update,
                    options,
                    cancellationToken);
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            document = await this.collection.Find(filter)
                .FirstAsync(cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            document = await this.collection.Find(filter)
                .FirstAsync(cancellationToken);
        }
        return ToModel(document);
    }

    public async Task<AccountDeletionOperation?> GetAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        AccountDeletionOperationDocument? document = await this.collection
            .Find(existing => existing.Id == operationId)
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToModel(document);
    }

    public async Task DeleteAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        _ = await this.collection.DeleteOneAsync(
            existing => existing.Id == operationId,
            cancellationToken);
    }

    private static AccountDeletionOperation ToModel(
        AccountDeletionOperationDocument document)
    {
        return new AccountDeletionOperation(
            document.Id,
            document.UserId,
            document.CreatedAt);
    }
}
