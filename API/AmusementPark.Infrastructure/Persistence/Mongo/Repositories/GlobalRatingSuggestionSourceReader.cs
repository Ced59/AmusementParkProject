using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class GlobalRatingSuggestionSourceReader
    : IGlobalRatingSuggestionSourceReader
{
    private readonly IMongoCollection<UserRatingDocument> ratings;
    private readonly IMongoCollection<UserVisitDocument> visits;
    private readonly IMongoCollection<UserRideOccurrenceDocument> occurrences;

    public GlobalRatingSuggestionSourceReader(
        IMongoDatabase database,
        MongoDbSettings settings)
        : this(
            database.GetCollection<UserRatingDocument>(settings.UserRatingsCollectionName),
            database.GetCollection<UserVisitDocument>(settings.UserVisitsCollectionName),
            database.GetCollection<UserRideOccurrenceDocument>(
                settings.UserRideOccurrencesCollectionName))
    {
    }

    internal GlobalRatingSuggestionSourceReader(
        IMongoCollection<UserRatingDocument> ratings,
        IMongoCollection<UserVisitDocument> visits,
        IMongoCollection<UserRideOccurrenceDocument> occurrences)
    {
        this.ratings = ratings ?? throw new ArgumentNullException(nameof(ratings));
        this.visits = visits ?? throw new ArgumentNullException(nameof(visits));
        this.occurrences = occurrences ?? throw new ArgumentNullException(nameof(occurrences));
    }

    public async Task<IReadOnlyCollection<GlobalRatingSuggestionSource>> ReadAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        List<GlobalRatingSuggestionRatingSourceDocument> ratingSources = await this.ratings
            .Find(BuildRatingFilter(normalizedUserId))
            .Project(static document => new GlobalRatingSuggestionRatingSourceDocument
            {
                TargetType = document.TargetType,
                TargetId = document.TargetId,
                ParkId = document.ParkId,
                ParkItemCategory = document.ParkItemCategory,
                ParkItemType = document.ParkItemType,
                Value = document.Value,
                UpdatedAtUtc = document.UpdatedAt,
            })
            .ToListAsync(cancellationToken);
        if (ratingSources.Count == 0)
        {
            return Array.Empty<GlobalRatingSuggestionSource>();
        }

        string[] parkIds = ratingSources.Select(static rating => rating.ParkId)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        List<GlobalRatingSuggestionVisitSourceDocument> visitSources = parkIds.Length == 0
            ? new List<GlobalRatingSuggestionVisitSourceDocument>()
            : await this.visits.Find(
                    Builders<UserVisitDocument>.Filter.Eq(
                        static document => document.UserId,
                        normalizedUserId)
                    & Builders<UserVisitDocument>.Filter.In(
                        static document => document.ParkId,
                        parkIds)
                    & Builders<UserVisitDocument>.Filter.Ne(
                        static document => document.Status,
                        AmusementPark.Core.Domain.Visits.VisitStatus.Archived)
                    & UserVisitMongoDefinitions.BuildNotDeletedFilter())
                .Project(static document => new GlobalRatingSuggestionVisitSourceDocument
                {
                    Id = document.Id,
                    ParkId = document.ParkId,
                    ContentMutationFenceToken = document.ContentMutationFenceToken,
                    ContentMutationFenceStableToken = document.ContentMutationFenceStableToken,
                    ContentMutationFenceReady = document.ContentMutationFenceReady,
                    AssessmentValueHalfSteps = document.ParkAssessment == null
                        ? null
                        : document.ParkAssessment.ValueHalfSteps,
                    AssessmentUpdatedAtUtc = document.ParkAssessment == null
                        ? null
                        : document.ParkAssessment.UpdatedAtUtc,
                })
                .ToListAsync(cancellationToken);

        string[] parkItemIds = ratingSources
            .Where(static rating => rating.TargetType == RatingTargetType.ParkItem)
            .Select(static rating => rating.TargetId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] activeVisitIds = visitSources
            .Select(static visit => visit.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        List<GlobalRatingSuggestionOccurrenceSourceDocument> occurrenceSources =
            parkItemIds.Length == 0 || activeVisitIds.Length == 0
                ? new List<GlobalRatingSuggestionOccurrenceSourceDocument>()
                : await this.occurrences.Find(
                        BuildOccurrenceFilter(
                            normalizedUserId,
                            activeVisitIds,
                            parkItemIds))
                    .Project(static document => new GlobalRatingSuggestionOccurrenceSourceDocument
                    {
                        VisitId = document.VisitId,
                        ParkId = document.ParkId,
                        ParkItemId = document.ParkItemId,
                        ContentMutationFenceToken = document.ContentMutationFenceToken,
                        AssessmentValueHalfSteps = document.Assessment == null
                            ? null
                            : document.Assessment.ValueHalfSteps,
                        AssessmentUpdatedAtUtc = document.Assessment == null
                            ? null
                            : document.Assessment.UpdatedAtUtc,
                    })
                    .ToListAsync(cancellationToken);

        return BuildSources(ratingSources, visitSources, occurrenceSources);
    }

    internal static FilterDefinition<UserRatingDocument> BuildRatingFilter(string userId)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<UserRatingDocument> filters =
            Builders<UserRatingDocument>.Filter;
        return filters.Eq(static document => document.UserId, normalizedUserId)
            & filters.Ne(static document => document.IsMutationPlaceholder, true);
    }

    internal static FilterDefinition<UserRideOccurrenceDocument> BuildOccurrenceFilter(
        string userId,
        IReadOnlyCollection<string> activeVisitIds,
        IReadOnlyCollection<string> parkItemIds)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        ArgumentNullException.ThrowIfNull(activeVisitIds);
        ArgumentNullException.ThrowIfNull(parkItemIds);
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(static document => document.UserId, normalizedUserId)
            & filters.In(static document => document.VisitId, activeVisitIds)
            & filters.In(static document => document.ParkItemId, parkItemIds)
            & filters.Eq(static document => document.DeletedAtUtc, null)
            & filters.Ne(static document => document.CreationPendingCompletion, true)
            & filters.Ne(static document => document.Assessment, null);
    }

    internal static IReadOnlyCollection<GlobalRatingSuggestionSource> BuildSources(
        IReadOnlyCollection<GlobalRatingSuggestionRatingSourceDocument> ratings,
        IReadOnlyCollection<GlobalRatingSuggestionVisitSourceDocument> visits,
        IReadOnlyCollection<GlobalRatingSuggestionOccurrenceSourceDocument> occurrences)
    {
        ArgumentNullException.ThrowIfNull(ratings);
        ArgumentNullException.ThrowIfNull(visits);
        ArgumentNullException.ThrowIfNull(occurrences);
        Dictionary<string, GlobalRatingSuggestionVisitSourceDocument> visitsById =
            visits.ToDictionary(static visit => visit.Id, StringComparer.Ordinal);
        IReadOnlyDictionary<string, GlobalRatingSuggestionObservation[]> parkObservationsByTarget =
            BuildParkObservationIndex(visits);
        IReadOnlyDictionary<string, GlobalRatingSuggestionObservation[]> rideObservationsByTarget =
            BuildRideObservationIndex(occurrences, visitsById);
        List<GlobalRatingSuggestionSource> result =
            new List<GlobalRatingSuggestionSource>(ratings.Count);
        foreach (GlobalRatingSuggestionRatingSourceDocument rating in ratings)
        {
            if (!RatingValue.TryFromDouble(
                    rating.Value,
                    out RatingValue currentRating,
                    out string? unusedError))
            {
                continue;
            }

            IReadOnlyDictionary<string, GlobalRatingSuggestionObservation[]> observationIndex =
                rating.TargetType == RatingTargetType.Park
                    ? parkObservationsByTarget
                    : rideObservationsByTarget;
            IReadOnlyCollection<GlobalRatingSuggestionObservation> observations =
                observationIndex.TryGetValue(
                    rating.TargetId,
                    out GlobalRatingSuggestionObservation[]? indexedObservations)
                    ? indexedObservations
                    : Array.Empty<GlobalRatingSuggestionObservation>();
            result.Add(new GlobalRatingSuggestionSource(
                rating.TargetType,
                rating.TargetId,
                rating.ParkId,
                rating.ParkItemCategory,
                rating.ParkItemType,
                currentRating,
                EnsureUtc(rating.UpdatedAtUtc),
                observations));
        }

        return result;
    }

    private static IReadOnlyDictionary<string, GlobalRatingSuggestionObservation[]>
        BuildParkObservationIndex(
            IReadOnlyCollection<GlobalRatingSuggestionVisitSourceDocument> visits)
    {
        Dictionary<string, List<GlobalRatingSuggestionObservation>> grouped =
            new Dictionary<string, List<GlobalRatingSuggestionObservation>>(StringComparer.Ordinal);
        foreach (GlobalRatingSuggestionVisitSourceDocument visit in visits)
        {
            GlobalRatingSuggestionObservation? observation = ToParkObservation(visit);
            if (observation is null)
            {
                continue;
            }

            if (!grouped.TryGetValue(
                    visit.ParkId,
                    out List<GlobalRatingSuggestionObservation>? targetObservations))
            {
                targetObservations = new List<GlobalRatingSuggestionObservation>();
                grouped.Add(visit.ParkId, targetObservations);
            }

            targetObservations.Add(observation);
        }

        return grouped.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, GlobalRatingSuggestionObservation[]>
        BuildRideObservationIndex(
            IReadOnlyCollection<GlobalRatingSuggestionOccurrenceSourceDocument> occurrences,
            IReadOnlyDictionary<string, GlobalRatingSuggestionVisitSourceDocument> visitsById)
    {
        Dictionary<string, List<GlobalRatingSuggestionObservation>> grouped =
            new Dictionary<string, List<GlobalRatingSuggestionObservation>>(StringComparer.Ordinal);
        foreach (GlobalRatingSuggestionOccurrenceSourceDocument occurrence in occurrences)
        {
            if (!visitsById.TryGetValue(
                    occurrence.VisitId,
                    out GlobalRatingSuggestionVisitSourceDocument? visit)
                || !string.Equals(visit.ParkId, occurrence.ParkId, StringComparison.Ordinal)
                || !PassportStatisticsContentFence.AllowsRead(
                    visit.ContentMutationFenceToken,
                    visit.ContentMutationFenceStableToken,
                    visit.ContentMutationFenceReady,
                    occurrence.ContentMutationFenceToken))
            {
                continue;
            }

            GlobalRatingSuggestionObservation? observation = ToRideObservation(occurrence);
            if (observation is null)
            {
                continue;
            }

            if (!grouped.TryGetValue(
                    occurrence.ParkItemId,
                    out List<GlobalRatingSuggestionObservation>? targetObservations))
            {
                targetObservations = new List<GlobalRatingSuggestionObservation>();
                grouped.Add(occurrence.ParkItemId, targetObservations);
            }

            targetObservations.Add(observation);
        }

        return grouped.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    private static GlobalRatingSuggestionObservation? ToParkObservation(
        GlobalRatingSuggestionVisitSourceDocument source)
    {
        return ToObservation(
            source.AssessmentValueHalfSteps,
            source.AssessmentUpdatedAtUtc);
    }

    private static GlobalRatingSuggestionObservation? ToRideObservation(
        GlobalRatingSuggestionOccurrenceSourceDocument source)
    {
        return ToObservation(
            source.AssessmentValueHalfSteps,
            source.AssessmentUpdatedAtUtc);
    }

    private static GlobalRatingSuggestionObservation? ToObservation(
        byte? valueHalfSteps,
        DateTime? updatedAtUtc)
    {
        return valueHalfSteps is >= RatingValue.MinimumHalfSteps
            and <= RatingValue.MaximumHalfSteps
            && updatedAtUtc.HasValue
            ? new GlobalRatingSuggestionObservation(
                RatingValue.FromHalfSteps(valueHalfSteps.Value),
                EnsureUtc(updatedAtUtc.Value))
            : null;
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}

internal sealed class GlobalRatingSuggestionRatingSourceDocument
{
    public RatingTargetType TargetType { get; init; }

    public string TargetId { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public ParkItemCategory? ParkItemCategory { get; init; }

    public ParkItemType? ParkItemType { get; init; }

    public double Value { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

internal sealed class GlobalRatingSuggestionVisitSourceDocument
{
    public string Id { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public long? ContentMutationFenceToken { get; init; }

    public long? ContentMutationFenceStableToken { get; init; }

    public bool ContentMutationFenceReady { get; init; }

    public byte? AssessmentValueHalfSteps { get; init; }

    public DateTime? AssessmentUpdatedAtUtc { get; init; }
}

internal sealed class GlobalRatingSuggestionOccurrenceSourceDocument
{
    public string VisitId { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public string ParkItemId { get; init; } = string.Empty;

    public long? ContentMutationFenceToken { get; init; }

    public byte? AssessmentValueHalfSteps { get; init; }

    public DateTime? AssessmentUpdatedAtUtc { get; init; }
}
