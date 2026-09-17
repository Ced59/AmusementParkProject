using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class NotificationEmailPreferenceRepository
    : INotificationEmailPreferenceRepository
{
    private readonly IMongoCollection<NotificationEmailPreferenceDocument> collection;

    public NotificationEmailPreferenceRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal NotificationEmailPreferenceRepository(
        IMongoCollection<NotificationEmailPreferenceDocument> collection)
    {
        this.collection = collection;
    }

    public async Task<NotificationEmailPreference?> GetAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        NotificationEmailPreferenceDocument? document = await this.collection.Find(
            Builders<NotificationEmailPreferenceDocument>.Filter.Eq(
                static item => item.Id,
                normalizedUserId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<NotificationEmailPreferenceWriteOutcome> CreateAsync(
        NotificationEmailPreference preference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preference);
        try
        {
            await this.collection.InsertOneAsync(
                preference.ToDocument(),
                cancellationToken: cancellationToken);
            return NotificationEmailPreferenceWriteOutcome.Success;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return NotificationEmailPreferenceWriteOutcome.Conflict;
        }
    }

    public async Task<NotificationEmailPreferenceWriteOutcome> ReplaceAsync(
        NotificationEmailPreference preference,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preference);
        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            Builders<NotificationEmailPreferenceDocument>.Filter.Eq(
                static item => item.Id,
                preference.UserId)
            & Builders<NotificationEmailPreferenceDocument>.Filter.Eq(
                static item => item.Version,
                expectedVersion),
            preference.ToDocument(),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1
            ? NotificationEmailPreferenceWriteOutcome.Success
            : NotificationEmailPreferenceWriteOutcome.Conflict;
    }

    private static IMongoCollection<NotificationEmailPreferenceDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<NotificationEmailPreferenceDocument>(
            settings.NotificationPreferencesCollectionName);
    }
}
