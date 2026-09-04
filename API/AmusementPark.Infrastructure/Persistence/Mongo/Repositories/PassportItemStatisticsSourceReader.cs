using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class PassportItemStatisticsSourceReader
    : IPassportItemStatisticsSourceReader
{
    private readonly IMongoCollection<UserRideOccurrenceDocument> occurrenceCollection;
    private readonly IMongoCollection<UserVisitDocument> visitCollection;

    public PassportItemStatisticsSourceReader(
        IMongoDatabase database,
        MongoDbSettings settings)
        : this(
            database.GetCollection<UserRideOccurrenceDocument>(
                settings.UserRideOccurrencesCollectionName),
            database.GetCollection<UserVisitDocument>(settings.UserVisitsCollectionName))
    {
    }

    internal PassportItemStatisticsSourceReader(
        IMongoCollection<UserRideOccurrenceDocument> occurrenceCollection,
        IMongoCollection<UserVisitDocument> visitCollection)
    {
        this.occurrenceCollection = occurrenceCollection
            ?? throw new ArgumentNullException(nameof(occurrenceCollection));
        this.visitCollection = visitCollection
            ?? throw new ArgumentNullException(nameof(visitCollection));
    }

    public async Task<IReadOnlyCollection<PassportItemRideObservation>> ReadAsync(
        string userId,
        string parkItemId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string normalizedParkItemId = IdentifierRules.NormalizeRequired(
            parkItemId,
            nameof(parkItemId));
        List<PassportItemOccurrenceStatisticsSourceDocument> occurrenceSources =
            await this.occurrenceCollection
                .Find(BuildOccurrenceFilter(normalizedUserId, normalizedParkItemId))
                .Project(BuildOccurrenceProjection())
                .ToListAsync(cancellationToken);
        if (occurrenceSources.Count == 0)
        {
            return Array.Empty<PassportItemRideObservation>();
        }

        string[] visitIds = occurrenceSources
            .Select(static source => source.VisitId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        List<PassportItemVisitStatisticsSourceDocument> visitSources =
            await this.visitCollection
                .Find(BuildVisitFilter(normalizedUserId, visitIds))
                .Project(BuildVisitProjection())
                .ToListAsync(cancellationToken);
        IReadOnlyDictionary<string, PassportItemVisitStatisticsSourceDocument> visitsById =
            visitSources.ToDictionary(static source => source.Id, StringComparer.Ordinal);

        return BuildObservations(occurrenceSources, visitsById);
    }

    internal static FilterDefinition<UserRideOccurrenceDocument> BuildOccurrenceFilter(
        string userId,
        string parkItemId)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                IdentifierRules.NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(
                static document => document.ParkItemId,
                IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId)))
            & filters.Eq(
                static document => document.Status,
                RideOccurrenceStatus.Completed)
            & filters.Eq(static document => document.DeletedAtUtc, null)
            & filters.Ne(static document => document.CreationPendingCompletion, true);
    }

    internal static FilterDefinition<UserVisitDocument> BuildVisitFilter(
        string userId,
        IReadOnlyCollection<string> visitIds)
    {
        ArgumentNullException.ThrowIfNull(visitIds);
        FilterDefinitionBuilder<UserVisitDocument> filters =
            Builders<UserVisitDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                IdentifierRules.NormalizeRequired(userId, nameof(userId)))
            & filters.In(static document => document.Id, visitIds)
            & filters.Ne(static document => document.Status, VisitStatus.Archived)
            & UserVisitMongoDefinitions.BuildNotDeletedFilter();
    }

    internal static IReadOnlyCollection<PassportItemRideObservation> BuildObservations(
        IReadOnlyCollection<PassportItemOccurrenceStatisticsSourceDocument> occurrences,
        IReadOnlyDictionary<string, PassportItemVisitStatisticsSourceDocument> visitsById)
    {
        ArgumentNullException.ThrowIfNull(occurrences);
        ArgumentNullException.ThrowIfNull(visitsById);
        List<PassportItemRideObservation> observations =
            new List<PassportItemRideObservation>(occurrences.Count);
        foreach (PassportItemOccurrenceStatisticsSourceDocument occurrence in occurrences)
        {
            if (!visitsById.TryGetValue(
                    occurrence.VisitId,
                    out PassportItemVisitStatisticsSourceDocument? visit)
                || !ContentFenceAllowsRead(visit, occurrence.ContentMutationFenceToken))
            {
                continue;
            }

            RatingValue? assessment = occurrence.AssessmentValueHalfSteps.HasValue
                ? RatingValue.FromHalfSteps(occurrence.AssessmentValueHalfSteps.Value)
                : null;
            observations.Add(new PassportItemRideObservation(
                occurrence.Id,
                occurrence.VisitId,
                new VisitDate(
                    visit.Date.Year,
                    visit.Date.Month,
                    visit.Date.Day,
                    visit.Date.Precision,
                    visit.Date.IsApproximate),
                occurrence.SortPosition,
                assessment));
        }

        return observations;
    }

    internal static bool ContentFenceAllowsRead(
        PassportItemVisitStatisticsSourceDocument visit,
        long? occurrenceFenceToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return PassportStatisticsContentFence.AllowsRead(
            visit.ContentMutationFenceToken,
            visit.ContentMutationFenceStableToken,
            visit.ContentMutationFenceReady,
            occurrenceFenceToken);
    }

    private static ProjectionDefinition<
        UserRideOccurrenceDocument,
        PassportItemOccurrenceStatisticsSourceDocument> BuildOccurrenceProjection()
    {
        return Builders<UserRideOccurrenceDocument>.Projection.Expression(
            static document => new PassportItemOccurrenceStatisticsSourceDocument
            {
                Id = document.Id,
                VisitId = document.VisitId,
                SortPosition = document.SortPosition,
                AssessmentValueHalfSteps = document.Assessment == null
                    ? null
                    : document.Assessment.ValueHalfSteps,
                ContentMutationFenceToken = document.ContentMutationFenceToken,
            });
    }

    private static ProjectionDefinition<
        UserVisitDocument,
        PassportItemVisitStatisticsSourceDocument> BuildVisitProjection()
    {
        return Builders<UserVisitDocument>.Projection.Expression(
            static document => new PassportItemVisitStatisticsSourceDocument
            {
                Id = document.Id,
                Date = document.Date,
                ContentMutationFenceToken = document.ContentMutationFenceToken,
                ContentMutationFenceStableToken = document.ContentMutationFenceStableToken,
                ContentMutationFenceReady = document.ContentMutationFenceReady,
            });
    }
}

internal sealed class PassportItemOccurrenceStatisticsSourceDocument
{
    public string Id { get; init; } = string.Empty;

    public string VisitId { get; init; } = string.Empty;

    public long SortPosition { get; init; }

    public byte? AssessmentValueHalfSteps { get; init; }

    public long? ContentMutationFenceToken { get; init; }
}

internal sealed class PassportItemVisitStatisticsSourceDocument
{
    public string Id { get; init; } = string.Empty;

    public VisitDateDocument Date { get; init; } = new VisitDateDocument();

    public long? ContentMutationFenceToken { get; init; }

    public long? ContentMutationFenceStableToken { get; init; }

    public bool ContentMutationFenceReady { get; init; }
}
