using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Sharing.Models;
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
    private readonly IMongoCollection<VisitRecapShareSnapshotDocument> visitSnapshots;
    private readonly IMongoCollection<YearRecapShareSnapshotDocument> yearSnapshots;
    private readonly IMongoCollection<PassportProfileShareSnapshotDocument> passportSnapshots;

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
        this.visitSnapshots = database.GetCollection<VisitRecapShareSnapshotDocument>(
            settings.SharePublicationSnapshotsCollectionName);
        this.yearSnapshots = database.GetCollection<YearRecapShareSnapshotDocument>(
            settings.SharePublicationSnapshotsCollectionName);
        this.passportSnapshots = database.GetCollection<PassportProfileShareSnapshotDocument>(
            settings.SharePublicationSnapshotsCollectionName);
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
        IReadOnlyCollection<VisitRecapShareSnapshot> loadedVisitSnapshots =
            await this.LoadVisitSnapshotsAsync(
                loadedPublications,
                sourceBudget,
                cancellationToken);
        IReadOnlyCollection<YearRecapShareSnapshot> loadedYearSnapshots =
            await this.LoadYearSnapshotsAsync(
                loadedPublications,
                sourceBudget,
                cancellationToken);
        IReadOnlyCollection<PassportProfileShareSnapshot> loadedPassportSnapshots =
            await this.LoadPassportSnapshotsAsync(
                loadedPublications,
                sourceBudget,
                cancellationToken);
        return new PassportShareLifecycleExportData(
            loadedPublications,
            loadedInvitations,
            loadedComparisons,
            loadedVisitSnapshots,
            loadedYearSnapshots,
            loadedPassportSnapshots);
    }

    private async Task<IReadOnlyCollection<SharePublication>> LoadPublicationsAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        List<SharePublication> result = new List<SharePublication>();
        using IAsyncCursor<SharePublicationDocument> cursor = await this.publications
            .Find(SharePublicationMongoDefinitions.BuildOwnerExportFilter(userId))
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (SharePublicationDocument document in cursor.Current)
            {
                Consume(sourceBudget, document.ToBson().LongLength);
                result.Add(document.ToDomain());
            }
        }

        return result
            .OrderBy(static publication => publication.CreatedAtUtc)
            .ThenBy(static publication => publication.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<ProfileComparisonInvitation>> LoadInvitationsAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        List<ProfileComparisonInvitation> result = new List<ProfileComparisonInvitation>();
        using IAsyncCursor<ProfileComparisonInvitationDocument> cursor = await this.invitations
            .Find(ProfileComparisonInvitationMongoDefinitions.BuildParticipantExportFilter(userId))
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (ProfileComparisonInvitationDocument document in cursor.Current)
            {
                Consume(sourceBudget, document.ToBson().LongLength);
                result.Add(document.ToDomain());
            }
        }

        return result
            .OrderBy(static invitation => invitation.CreatedAtUtc)
            .ThenBy(static invitation => invitation.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<ProfileComparison>> LoadComparisonsAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        List<ProfileComparison> result = new List<ProfileComparison>();
        using IAsyncCursor<ProfileComparisonDocument> cursor = await this.comparisons
            .Find(ProfileComparisonMongoDefinitions.BuildParticipantExportFilter(userId))
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (ProfileComparisonDocument document in cursor.Current)
            {
                Consume(sourceBudget, document.ToBson().LongLength);
                result.Add(document.ToDomain());
            }
        }

        return result
            .OrderBy(static comparison => comparison.CreatedAtUtc)
            .ThenBy(static comparison => comparison.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<VisitRecapShareSnapshot>> LoadVisitSnapshotsAsync(
        IReadOnlyCollection<SharePublication> loadedPublications,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> documentIds = BuildSnapshotDocumentIds(
            loadedPublications,
            SharePublicationType.VisitRecap);
        if (documentIds.Count == 0)
        {
            return Array.Empty<VisitRecapShareSnapshot>();
        }

        List<VisitRecapShareSnapshot> result = new List<VisitRecapShareSnapshot>();
        FilterDefinition<VisitRecapShareSnapshotDocument> filter =
            Builders<VisitRecapShareSnapshotDocument>.Filter.In(
                static document => document.Id,
                documentIds);
        using IAsyncCursor<VisitRecapShareSnapshotDocument> cursor = await this.visitSnapshots
            .Find(filter)
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (VisitRecapShareSnapshotDocument document in cursor.Current)
            {
                Consume(sourceBudget, document.ToBson().LongLength);
                result.Add(document.ToDomain());
            }
        }

        return result
            .OrderBy(static snapshot => snapshot.CreatedAtUtc)
            .ThenBy(static snapshot => snapshot.PublicationId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<YearRecapShareSnapshot>> LoadYearSnapshotsAsync(
        IReadOnlyCollection<SharePublication> loadedPublications,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> documentIds = BuildSnapshotDocumentIds(
            loadedPublications,
            SharePublicationType.YearRecap);
        if (documentIds.Count == 0)
        {
            return Array.Empty<YearRecapShareSnapshot>();
        }

        List<YearRecapShareSnapshot> result = new List<YearRecapShareSnapshot>();
        FilterDefinition<YearRecapShareSnapshotDocument> filter =
            Builders<YearRecapShareSnapshotDocument>.Filter.In(
                static document => document.Id,
                documentIds);
        using IAsyncCursor<YearRecapShareSnapshotDocument> cursor = await this.yearSnapshots
            .Find(filter)
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (YearRecapShareSnapshotDocument document in cursor.Current)
            {
                Consume(sourceBudget, document.ToBson().LongLength);
                result.Add(document.ToDomain());
            }
        }

        return result
            .OrderBy(static snapshot => snapshot.CreatedAtUtc)
            .ThenBy(static snapshot => snapshot.PublicationId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<PassportProfileShareSnapshot>> LoadPassportSnapshotsAsync(
        IReadOnlyCollection<SharePublication> loadedPublications,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> documentIds = BuildSnapshotDocumentIds(
            loadedPublications,
            SharePublicationType.PassportProfile);
        if (documentIds.Count == 0)
        {
            return Array.Empty<PassportProfileShareSnapshot>();
        }

        List<PassportProfileShareSnapshot> result = new List<PassportProfileShareSnapshot>();
        FilterDefinition<PassportProfileShareSnapshotDocument> filter =
            Builders<PassportProfileShareSnapshotDocument>.Filter.In(
                static document => document.Id,
                documentIds);
        using IAsyncCursor<PassportProfileShareSnapshotDocument> cursor = await this.passportSnapshots
            .Find(filter)
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (PassportProfileShareSnapshotDocument document in cursor.Current)
            {
                Consume(sourceBudget, document.ToBson().LongLength);
                result.Add(document.ToDomain());
            }
        }

        return result
            .OrderBy(static snapshot => snapshot.CreatedAtUtc)
            .ThenBy(static snapshot => snapshot.PublicationId.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyCollection<string> BuildSnapshotDocumentIds(
        IEnumerable<SharePublication> loadedPublications,
        SharePublicationType publicationType)
    {
        return loadedPublications
            .Where(publication =>
                publication.Type == publicationType &&
                publication.PublicationVersion > 0)
            .Select(publication => publicationType switch
            {
                SharePublicationType.VisitRecap =>
                    VisitRecapShareSnapshotMongoMapper.CreateDocumentId(
                        publication.Id.Value,
                        publication.PublicationVersion),
                SharePublicationType.YearRecap =>
                    YearRecapShareSnapshotMongoMapper.CreateDocumentId(
                        publication.Id.Value,
                        publication.PublicationVersion),
                SharePublicationType.PassportProfile =>
                    PassportProfileShareSnapshotMongoMapper.CreateDocumentId(
                        publication.Id.Value,
                        publication.PublicationVersion),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(publicationType),
                    publicationType,
                    "Unsupported snapshot publication type."),
            })
            .ToArray();
    }

    private static void Consume(PassportExportSourceBudget sourceBudget, long bytes)
    {
        if (!sourceBudget.TryConsume(bytes))
        {
            throw new PassportExportSizeLimitException();
        }
    }
}
