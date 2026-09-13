using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.Passport;

public sealed class MongoPassportShareLifecycleExportSource : IPassportShareLifecycleExportSource
{
    private readonly IMongoCollection<SharePublicationDocument> publications;
    private readonly IMongoCollection<ProfileComparisonInvitationDocument> invitations;
    private readonly IMongoCollection<ProfileComparisonDocument> comparisons;

    public MongoPassportShareLifecycleExportSource(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.publications = database.GetCollection<SharePublicationDocument>(
            settings.SharePublicationsCollectionName);
        this.invitations = database.GetCollection<ProfileComparisonInvitationDocument>(
            settings.ProfileComparisonInvitationsCollectionName);
        this.comparisons = database.GetCollection<ProfileComparisonDocument>(
            settings.ProfileComparisonsCollectionName);
    }

    public async Task<PassportShareLifecycleExportData> LoadAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = userId?.Trim() ?? string.Empty;
        if (normalizedUserId.Length == 0)
        {
            throw new ArgumentException("A passport owner identifier is required.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(sourceBudget);
        IReadOnlyCollection<SharePublication> loadedPublications =
            await this.LoadPublicationsAsync(
                normalizedUserId,
                sourceBudget,
                cancellationToken);
        IReadOnlyCollection<ProfileComparisonInvitation> loadedInvitations =
            await this.LoadInvitationsAsync(
                normalizedUserId,
                sourceBudget,
                cancellationToken);
        IReadOnlyCollection<ProfileComparison> loadedComparisons =
            await this.LoadComparisonsAsync(
                normalizedUserId,
                sourceBudget,
                cancellationToken);
        return new PassportShareLifecycleExportData(
            loadedPublications,
            loadedInvitations,
            loadedComparisons);
    }

    private async Task<IReadOnlyCollection<SharePublication>> LoadPublicationsAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        List<SharePublication> result = new List<SharePublication>();
        using IAsyncCursor<SharePublicationDocument> cursor = await this.publications
            .Find(SharePublicationMongoDefinitions.BuildOwnerExportFilter(userId))
            .SortBy(static document => document.CreatedAt)
            .ThenBy(static document => document.Id)
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (SharePublicationDocument document in cursor.Current)
            {
                Consume(sourceBudget, document.ToBson().LongLength);
                result.Add(document.ToDomain());
            }
        }

        return result;
    }

    private async Task<IReadOnlyCollection<ProfileComparisonInvitation>> LoadInvitationsAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        List<ProfileComparisonInvitation> result = new List<ProfileComparisonInvitation>();
        using IAsyncCursor<ProfileComparisonInvitationDocument> cursor = await this.invitations
            .Find(ProfileComparisonInvitationMongoDefinitions.BuildParticipantExportFilter(userId))
            .SortBy(static document => document.CreatedAt)
            .ThenBy(static document => document.Id)
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (ProfileComparisonInvitationDocument document in cursor.Current)
            {
                Consume(sourceBudget, document.ToBson().LongLength);
                result.Add(document.ToDomain());
            }
        }

        return result;
    }

    private async Task<IReadOnlyCollection<ProfileComparison>> LoadComparisonsAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        List<ProfileComparison> result = new List<ProfileComparison>();
        using IAsyncCursor<ProfileComparisonDocument> cursor = await this.comparisons
            .Find(ProfileComparisonMongoDefinitions.BuildParticipantExportFilter(userId))
            .SortBy(static document => document.CreatedAt)
            .ThenBy(static document => document.Id)
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (ProfileComparisonDocument document in cursor.Current)
            {
                Consume(sourceBudget, document.ToBson().LongLength);
                result.Add(document.ToDomain());
            }
        }

        return result;
    }

    private static void Consume(PassportExportSourceBudget sourceBudget, long bytes)
    {
        if (!sourceBudget.TryConsume(bytes))
        {
            throw new PassportExportSizeLimitException();
        }
    }
}
