using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoVisitContentMutationLeaseManager :
    IVisitContentMutationLeaseManager
{
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    private readonly IMongoCollection<UserVisitDocument> collection;

    public MongoVisitContentMutationLeaseManager(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<UserVisitDocument>(
            settings.UserVisitsCollectionName);
    }

    public async Task<IVisitContentMutationLease?> TryAcquireAsync(
        Visit visit,
        DateTime acquiredAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        if (acquiredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The lease timestamp must be UTC.", nameof(acquiredAtUtc));
        }

        if (visit.Status != VisitStatus.Draft)
        {
            return null;
        }

        string token = Guid.NewGuid().ToString("N");
        FilterDefinitionBuilder<UserVisitDocument> filters =
            Builders<UserVisitDocument>.Filter;
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedVersionFilter(
                visit.Id.Value,
                visit.UserId,
                visit.Version)
            & filters.Eq(static document => document.Status, VisitStatus.Draft)
            & filters.Or(
                filters.Exists(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath, false),
                filters.Lte(
                    UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                    acquiredAtUtc));
        UpdateDefinition<UserVisitDocument> update =
            Builders<UserVisitDocument>.Update
                .Set(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath, token)
                .Set(
                    UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath,
                    acquiredAtUtc.Add(LeaseDuration));
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        return result.MatchedCount == 1
            ? new MongoVisitContentMutationLease(
                this.collection,
                visit.Id.Value,
                visit.UserId,
                token)
            : null;
    }

    private sealed class MongoVisitContentMutationLease : IVisitContentMutationLease
    {
        private readonly IMongoCollection<UserVisitDocument> collection;
        private readonly string visitId;
        private readonly string userId;
        private readonly string token;
        private int released;

        public MongoVisitContentMutationLease(
            IMongoCollection<UserVisitDocument> collection,
            string visitId,
            string userId,
            string token)
        {
            this.collection = collection;
            this.visitId = visitId;
            this.userId = userId;
            this.token = token;
        }

        public string Token => this.token;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref this.released, 1) != 0)
            {
                return;
            }

            FilterDefinitionBuilder<UserVisitDocument> filters =
                Builders<UserVisitDocument>.Filter;
            FilterDefinition<UserVisitDocument> filter = filters.Eq(
                    static document => document.Id,
                    this.visitId)
                & filters.Eq(static document => document.UserId, this.userId)
                & filters.Eq(
                    UserVisitMongoDefinitions.ContentMutationLeaseTokenPath,
                    this.token);
            UpdateDefinition<UserVisitDocument> update =
                Builders<UserVisitDocument>.Update
                    .Unset(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath)
                    .Unset(UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath);
            _ = await this.collection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = false },
                CancellationToken.None);
        }
    }
}
