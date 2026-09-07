using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class VisitRecapSourceReader : IVisitRecapSourceReader
{
    internal const int MaximumOccurrenceCount = 2000;

    private readonly IMongoCollection<UserVisitDocument> visits;
    private readonly IMongoCollection<UserRideOccurrenceDocument> occurrences;

    public VisitRecapSourceReader(IMongoDatabase database, MongoDbSettings settings)
        : this(
            database.GetCollection<UserVisitDocument>(settings.UserVisitsCollectionName),
            database.GetCollection<UserRideOccurrenceDocument>(
                settings.UserRideOccurrencesCollectionName))
    {
    }

    internal VisitRecapSourceReader(
        IMongoCollection<UserVisitDocument> visits,
        IMongoCollection<UserRideOccurrenceDocument> occurrences)
    {
        this.visits = visits ?? throw new ArgumentNullException(nameof(visits));
        this.occurrences = occurrences ?? throw new ArgumentNullException(nameof(occurrences));
    }

    public async Task<VisitRecapSourceRevision?> GetOwnedCompletedRevisionAsync(
        string ownerUserId,
        string visitId,
        CancellationToken cancellationToken)
    {
        UserVisitDocument? visit = await this.LoadVisitAsync(
            ownerUserId,
            visitId,
            includeContent: false,
            cancellationToken);
        return visit is null ? null : BuildRevision(visit);
    }

    public async Task<VisitRecapSourceData?> GetOwnedCompletedAsync(
        string ownerUserId,
        string visitId,
        CancellationToken cancellationToken)
    {
        UserVisitDocument? visit = await this.LoadVisitAsync(
            ownerUserId,
            visitId,
            includeContent: true,
            cancellationToken);
        if (visit is null)
        {
            return null;
        }

        VisitRecapSourceRevision revision = BuildRevision(visit);
        List<UserRideOccurrenceDocument> occurrenceDocuments = await this.occurrences
            .Find(BuildOccurrenceFilter(ownerUserId, visitId))
            .SortBy(static value => value.SortPosition)
            .Limit(MaximumOccurrenceCount + 1)
            .Project<UserRideOccurrenceDocument>(BuildOccurrenceProjection())
            .ToListAsync(cancellationToken);
        bool isComplete = occurrenceDocuments.Count <= MaximumOccurrenceCount;
        if (!isComplete)
        {
            occurrenceDocuments.RemoveAt(occurrenceDocuments.Count - 1);
        }

        bool occurrencesMatchFence = occurrenceDocuments.All(
            occurrence => PassportStatisticsContentFence.AllowsRead(
                visit.ContentMutationFenceToken,
                visit.ContentMutationFenceStableToken,
                visit.ContentMutationFenceReady,
                occurrence.ContentMutationFenceToken));
        revision = revision with { IsStable = revision.IsStable && occurrencesMatchFence };
        IReadOnlyCollection<VisitRecapSourceOccurrence> sourceOccurrences = occurrenceDocuments
            .Select(ToSourceOccurrence)
            .ToArray();
        VisitDate sourceDate = new VisitDate(
            visit.Date.Year,
            visit.Date.Month,
            visit.Date.Day,
            visit.Date.Precision,
            visit.Date.IsApproximate);
        RatingValue? parkRating = visit.ParkAssessment is null
            ? null
            : RatingValue.FromHalfSteps(visit.ParkAssessment.ValueHalfSteps);
        return new VisitRecapSourceData(
            visit.ParkId,
            sourceDate,
            parkRating,
            revision,
            sourceOccurrences,
            isComplete);
    }

    private async Task<UserVisitDocument?> LoadVisitAsync(
        string ownerUserId,
        string visitId,
        bool includeContent,
        CancellationToken cancellationToken)
    {
        ProjectionDefinition<UserVisitDocument> projection = BuildVisitProjection(includeContent);
        return await this.visits
            .Find(UserVisitMongoDefinitions.BuildOwnedVisitFilter(
                    visitId?.Trim() ?? string.Empty,
                    ownerUserId?.Trim() ?? string.Empty)
                & Builders<UserVisitDocument>.Filter.Eq(
                    static value => value.Status,
                    VisitStatus.Completed))
            .Project<UserVisitDocument>(projection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static VisitRecapSourceRevision BuildRevision(UserVisitDocument visit)
    {
        bool hasFence = visit.ContentMutationFenceToken.HasValue;
        bool isStable = string.IsNullOrWhiteSpace(visit.ContentMutationLeaseToken)
            && (!hasFence
                || visit.ContentMutationFenceReady
                && visit.ContentMutationFenceStableToken == visit.ContentMutationFenceToken);
        long version;
        try
        {
            version = checked(visit.Version + (visit.ContentMutationFenceToken ?? 0));
        }
        catch (OverflowException)
        {
            version = long.MaxValue;
            isStable = false;
        }

        return new VisitRecapSourceRevision(version, isStable);
    }

    private static VisitRecapSourceOccurrence ToSourceOccurrence(
        UserRideOccurrenceDocument occurrence)
    {
        ParkItemCategory? historicalCategory = Enum.TryParse(
            occurrence.HistoricalTarget?.Category,
            ignoreCase: true,
            out ParkItemCategory parsedCategory)
            ? parsedCategory
            : null;
        RatingValue? rating = occurrence.Assessment is null
            ? null
            : RatingValue.FromHalfSteps(occurrence.Assessment.ValueHalfSteps);
        return new VisitRecapSourceOccurrence(
            occurrence.ParkItemId,
            occurrence.Status,
            NormalizeOptional(occurrence.HistoricalTarget?.Name),
            historicalCategory,
            rating);
    }

    private static FilterDefinition<UserRideOccurrenceDocument> BuildOccurrenceFilter(
        string ownerUserId,
        string visitId)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(static value => value.UserId, ownerUserId.Trim())
            & filters.Eq(static value => value.VisitId, visitId.Trim())
            & filters.Eq(static value => value.DeletedAtUtc, null);
    }

    private static ProjectionDefinition<UserVisitDocument> BuildVisitProjection(
        bool includeContent)
    {
        ProjectionDefinitionBuilder<UserVisitDocument> projection =
            Builders<UserVisitDocument>.Projection;
        List<ProjectionDefinition<UserVisitDocument>> fields = new List<ProjectionDefinition<UserVisitDocument>>
        {
            projection.Include(static value => value.Id),
            projection.Include(static value => value.Version),
            projection.Include(static value => value.Status),
            projection.Include(static value => value.ContentMutationLeaseToken),
            projection.Include(static value => value.ContentMutationFenceToken),
            projection.Include(static value => value.ContentMutationFenceStableToken),
            projection.Include(static value => value.ContentMutationFenceReady),
        };
        if (includeContent)
        {
            fields.Add(projection.Include(static value => value.ParkId));
            fields.Add(projection.Include(static value => value.Date));
            fields.Add(projection.Include("parkAssessment.valueHalfSteps"));
        }

        return projection.Combine(fields);
    }

    private static ProjectionDefinition<UserRideOccurrenceDocument> BuildOccurrenceProjection()
    {
        ProjectionDefinitionBuilder<UserRideOccurrenceDocument> projection =
            Builders<UserRideOccurrenceDocument>.Projection;
        return projection.Combine(
            projection.Include(static value => value.ParkItemId),
            projection.Include(static value => value.SortPosition),
            projection.Include(static value => value.Status),
            projection.Include(static value => value.ContentMutationFenceToken),
            projection.Include("historicalTarget.name"),
            projection.Include("historicalTarget.category"),
            projection.Include("assessment.valueHalfSteps"));
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }
}
