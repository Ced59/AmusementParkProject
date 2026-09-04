using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class PassportScopeStatisticsSourceReader
    : IPassportScopeStatisticsSourceReader
{
    private readonly IMongoCollection<UserVisitDocument> visitCollection;
    private readonly IMongoCollection<UserRideOccurrenceDocument> occurrenceCollection;
    private readonly IMongoCollection<ParkItemDocument> parkItemCollection;
    private readonly IMongoCollection<UserRatingDocument> ratingCollection;

    public PassportScopeStatisticsSourceReader(
        IMongoDatabase database,
        MongoDbSettings settings)
        : this(
            database.GetCollection<UserVisitDocument>(settings.UserVisitsCollectionName),
            database.GetCollection<UserRideOccurrenceDocument>(
                settings.UserRideOccurrencesCollectionName),
            database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName),
            database.GetCollection<UserRatingDocument>(settings.UserRatingsCollectionName))
    {
    }

    internal PassportScopeStatisticsSourceReader(
        IMongoCollection<UserVisitDocument> visitCollection,
        IMongoCollection<UserRideOccurrenceDocument> occurrenceCollection,
        IMongoCollection<ParkItemDocument> parkItemCollection,
        IMongoCollection<UserRatingDocument> ratingCollection)
    {
        this.visitCollection = visitCollection
            ?? throw new ArgumentNullException(nameof(visitCollection));
        this.occurrenceCollection = occurrenceCollection
            ?? throw new ArgumentNullException(nameof(occurrenceCollection));
        this.parkItemCollection = parkItemCollection
            ?? throw new ArgumentNullException(nameof(parkItemCollection));
        this.ratingCollection = ratingCollection
            ?? throw new ArgumentNullException(nameof(ratingCollection));
    }

    public async Task<PassportParkStatisticsSource> ReadParkAsync(
        string userId,
        string parkId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string normalizedParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        Task<List<PassportScopeVisitSourceDocument>> visitsTask = this.visitCollection
            .Find(PassportScopeStatisticsMongoDefinitions.BuildParkVisitFilter(
                normalizedUserId,
                normalizedParkId))
            .Project(PassportScopeStatisticsMongoDefinitions.BuildVisitProjection())
            .ToListAsync(cancellationToken);
        Task<List<PassportScopeRatingSourceDocument>> ratingsTask = this.ratingCollection
            .Find(PassportScopeStatisticsMongoDefinitions.BuildParkRatingFilter(
                normalizedUserId,
                normalizedParkId))
            .Project(PassportScopeStatisticsMongoDefinitions.BuildRatingProjection())
            .ToListAsync(cancellationToken);
        await Task.WhenAll(visitsTask, ratingsTask);

        List<PassportScopeVisitSourceDocument> visits = await visitsTask;
        PassportScopeRideSources rideSources = await this.ReadRideSourcesAsync(
            normalizedUserId,
            visits,
            cancellationToken);
        return BuildParkSource(
            normalizedParkId,
            visits,
            rideSources.Occurrences,
            rideSources.CurrentCategories,
            await ratingsTask);
    }

    public async Task<PassportYearStatisticsSource> ReadYearAsync(
        string userId,
        int year,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        if (year < DateOnly.MinValue.Year || year > DateOnly.MaxValue.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        List<PassportScopeVisitSourceDocument> visits = await this.visitCollection
            .Find(PassportScopeStatisticsMongoDefinitions.BuildYearVisitFilter(
                normalizedUserId,
                year))
            .Project(PassportScopeStatisticsMongoDefinitions.BuildVisitProjection())
            .ToListAsync(cancellationToken);
        PassportScopeRideSources rideSources = await this.ReadRideSourcesAsync(
            normalizedUserId,
            visits,
            cancellationToken);
        return new PassportYearStatisticsSource(
            BuildVisitObservations(visits),
            BuildRideObservations(
                rideSources.Occurrences,
                visits.ToDictionary(static visit => visit.Id, StringComparer.Ordinal),
                rideSources.CurrentCategories));
    }

    internal static PassportParkStatisticsSource BuildParkSource(
        string parkId,
        IReadOnlyCollection<PassportScopeVisitSourceDocument> visits,
        IReadOnlyCollection<PassportScopeOccurrenceSourceDocument> occurrences,
        IReadOnlyDictionary<string, string> currentCategories,
        IReadOnlyCollection<PassportScopeRatingSourceDocument> ratings)
    {
        string normalizedParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        ArgumentNullException.ThrowIfNull(visits);
        ArgumentNullException.ThrowIfNull(occurrences);
        ArgumentNullException.ThrowIfNull(currentCategories);
        ArgumentNullException.ThrowIfNull(ratings);
        PassportScopeRatingSourceDocument? global = ratings.FirstOrDefault(rating =>
            rating.TargetType == RatingTargetType.Park
            && string.Equals(rating.TargetId, normalizedParkId, StringComparison.Ordinal));
        return new PassportParkStatisticsSource(
            BuildVisitObservations(visits),
            BuildRideObservations(
                occurrences,
                visits.ToDictionary(static visit => visit.Id, StringComparer.Ordinal),
                currentCategories),
            global is null ? null : RatingValue.FromDouble(global.Value),
            ratings.Where(static rating => rating.TargetType == RatingTargetType.ParkItem)
                .Select(static rating => new PassportCurrentItemRatingObservation(
                    rating.TargetId,
                    RatingValue.FromDouble(rating.Value)))
                .ToArray());
    }

    internal static IReadOnlyCollection<PassportVisitStatisticsObservation>
        BuildVisitObservations(IReadOnlyCollection<PassportScopeVisitSourceDocument> visits)
    {
        ArgumentNullException.ThrowIfNull(visits);
        return visits.Select(static visit => new PassportVisitStatisticsObservation(
            visit.Id,
            visit.ParkId,
            ToVisitDate(visit.Date),
            visit.ParkAssessmentValueHalfSteps.HasValue
                ? RatingValue.FromHalfSteps(visit.ParkAssessmentValueHalfSteps.Value)
                : null)).ToArray();
    }

    internal static IReadOnlyCollection<PassportRideStatisticsObservation>
        BuildRideObservations(
            IReadOnlyCollection<PassportScopeOccurrenceSourceDocument> occurrences,
            IReadOnlyDictionary<string, PassportScopeVisitSourceDocument> visitsById,
            IReadOnlyDictionary<string, string> currentCategories)
    {
        ArgumentNullException.ThrowIfNull(occurrences);
        ArgumentNullException.ThrowIfNull(visitsById);
        ArgumentNullException.ThrowIfNull(currentCategories);
        List<PassportRideStatisticsObservation> observations =
            new List<PassportRideStatisticsObservation>(occurrences.Count);
        foreach (PassportScopeOccurrenceSourceDocument occurrence in occurrences)
        {
            if (!visitsById.TryGetValue(
                    occurrence.VisitId,
                    out PassportScopeVisitSourceDocument? visit)
                || !string.Equals(visit.ParkId, occurrence.ParkId, StringComparison.Ordinal)
                || !PassportStatisticsContentFence.AllowsRead(
                    visit.ContentMutationFenceToken,
                    visit.ContentMutationFenceStableToken,
                    visit.ContentMutationFenceReady,
                    occurrence.ContentMutationFenceToken))
            {
                continue;
            }

            currentCategories.TryGetValue(occurrence.ParkItemId, out string? currentCategory);
            observations.Add(new PassportRideStatisticsObservation(
                occurrence.Id,
                occurrence.VisitId,
                occurrence.ParkId,
                occurrence.ParkItemId,
                ToVisitDate(visit.Date),
                occurrence.Status,
                occurrence.AssessmentValueHalfSteps.HasValue
                    ? RatingValue.FromHalfSteps(occurrence.AssessmentValueHalfSteps.Value)
                    : null,
                occurrence.HistoricalCategory,
                currentCategory));
        }

        return observations;
    }

    private async Task<PassportScopeRideSources> ReadRideSourcesAsync(
        string userId,
        IReadOnlyCollection<PassportScopeVisitSourceDocument> visits,
        CancellationToken cancellationToken)
    {
        if (visits.Count == 0)
        {
            return new PassportScopeRideSources(
                Array.Empty<PassportScopeOccurrenceSourceDocument>(),
                new Dictionary<string, string>(StringComparer.Ordinal));
        }

        string[] visitIds = visits.Select(static visit => visit.Id).ToArray();
        List<PassportScopeOccurrenceSourceDocument> occurrences =
            await this.occurrenceCollection
                .Find(PassportScopeStatisticsMongoDefinitions.BuildOccurrenceFilter(
                    userId,
                    visitIds))
                .Project(PassportScopeStatisticsMongoDefinitions.BuildOccurrenceProjection())
                .ToListAsync(cancellationToken);
        string[] parkItemIds = occurrences.Select(static occurrence => occurrence.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (parkItemIds.Length == 0)
        {
            return new PassportScopeRideSources(
                occurrences,
                new Dictionary<string, string>(StringComparer.Ordinal));
        }

        List<PassportScopeParkItemSourceDocument> parkItems = await this.parkItemCollection
            .Find(PassportScopeStatisticsMongoDefinitions.BuildParkItemFilter(parkItemIds))
            .Project(PassportScopeStatisticsMongoDefinitions.BuildParkItemProjection())
            .ToListAsync(cancellationToken);
        return new PassportScopeRideSources(
            occurrences,
            parkItems.ToDictionary(
                static item => item.Id,
                static item => item.Category.ToString(),
                StringComparer.Ordinal));
    }

    private static VisitDate ToVisitDate(VisitDateDocument date)
    {
        return new VisitDate(
            date.Year,
            date.Month,
            date.Day,
            date.Precision,
            date.IsApproximate);
    }
}

internal sealed record PassportScopeRideSources(
    IReadOnlyCollection<PassportScopeOccurrenceSourceDocument> Occurrences,
    IReadOnlyDictionary<string, string> CurrentCategories);
