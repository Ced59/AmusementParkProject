using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class YearRecapSourceReader : IYearRecapSourceReader
{
    private readonly IMongoCollection<UserVisitDocument> visits;
    private readonly IMongoCollection<UserRideOccurrenceDocument> occurrences;

    public YearRecapSourceReader(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.visits = database.GetCollection<UserVisitDocument>(settings.UserVisitsCollectionName);
        this.occurrences = database.GetCollection<UserRideOccurrenceDocument>(
            settings.UserRideOccurrencesCollectionName);
    }

    public async Task<YearRecapSourceData> ReadOwnedCompletedYearAsync(
        string ownerUserId,
        int year,
        CancellationToken cancellationToken)
    {
        string normalizedOwner = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        if (year < DateOnly.MinValue.Year || year > DateOnly.MaxValue.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        FilterDefinitionBuilder<UserVisitDocument> visitFilters =
            Builders<UserVisitDocument>.Filter;
        FilterDefinition<UserVisitDocument> visitFilter = visitFilters.Eq(
                static value => value.UserId,
                normalizedOwner)
            & visitFilters.Eq(static value => value.Date.Year, year)
            & visitFilters.Eq(static value => value.Status, VisitStatus.Completed)
            & UserVisitMongoDefinitions.BuildNotDeletedFilter();
        List<YearRecapVisitSourceDocument> visitDocuments = await this.visits
            .Find(visitFilter)
            .Project(BuildVisitProjection())
            .ToListAsync(cancellationToken);
        if (visitDocuments.Count == 0)
        {
            return new YearRecapSourceData(
                Array.Empty<PassportVisitStatisticsObservation>(),
                Array.Empty<PassportRideStatisticsObservation>(),
                new Dictionary<string, string?>(StringComparer.Ordinal),
                ComputeFingerprint(visitDocuments, Array.Empty<YearRecapRideSourceDocument>()),
                true);
        }

        string[] visitIds = visitDocuments.Select(static value => value.Id).ToArray();
        FilterDefinitionBuilder<UserRideOccurrenceDocument> occurrenceFilters =
            Builders<UserRideOccurrenceDocument>.Filter;
        FilterDefinition<UserRideOccurrenceDocument> occurrenceFilter = occurrenceFilters.Eq(
                static value => value.UserId,
                normalizedOwner)
            & occurrenceFilters.In(static value => value.VisitId, visitIds)
            & occurrenceFilters.Eq(static value => value.DeletedAtUtc, null)
            & occurrenceFilters.Ne(static value => value.CreationPendingCompletion, true);
        List<YearRecapRideSourceDocument> occurrenceDocuments = await this.occurrences
            .Find(occurrenceFilter)
            .Project(BuildOccurrenceProjection())
            .ToListAsync(cancellationToken);
        Dictionary<string, YearRecapVisitSourceDocument> visitsById = visitDocuments
            .ToDictionary(static value => value.Id, StringComparer.Ordinal);
        bool isStable = visitDocuments.All(IsStable)
            && occurrenceDocuments.All(occurrence =>
                visitsById.TryGetValue(
                    occurrence.VisitId,
                    out YearRecapVisitSourceDocument? visit)
                && string.Equals(visit.ParkId, occurrence.ParkId, StringComparison.Ordinal)
                && PassportStatisticsContentFence.AllowsRead(
                    visit.ContentMutationFenceToken,
                    visit.ContentMutationFenceStableToken,
                    visit.ContentMutationFenceReady,
                    occurrence.ContentMutationFenceToken));
        PassportVisitStatisticsObservation[] visitObservations = visitDocuments
            .Select(static value => new PassportVisitStatisticsObservation(
                value.Id,
                value.ParkId,
                ToVisitDate(value.Date),
                value.ParkAssessmentValueHalfSteps.HasValue
                    ? RatingValue.FromHalfSteps(value.ParkAssessmentValueHalfSteps.Value)
                    : null))
            .ToArray();
        PassportRideStatisticsObservation[] rideObservations = occurrenceDocuments
            .Where(occurrence => visitsById.ContainsKey(occurrence.VisitId))
            .Select(occurrence => new PassportRideStatisticsObservation(
                occurrence.Id,
                occurrence.VisitId,
                occurrence.ParkId,
                occurrence.ParkItemId,
                ToVisitDate(visitsById[occurrence.VisitId].Date),
                occurrence.Status,
                occurrence.AssessmentValueHalfSteps.HasValue
                    ? RatingValue.FromHalfSteps(occurrence.AssessmentValueHalfSteps.Value)
                    : null,
                occurrence.HistoricalCategory,
                null))
            .ToArray();
        IReadOnlyDictionary<string, string?> historicalNames = occurrenceDocuments
            .GroupBy(static value => value.ParkItemId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static value => NormalizeOptional(value.HistoricalName))
                    .FirstOrDefault(static value => value is not null),
                StringComparer.Ordinal);
        return new YearRecapSourceData(
            visitObservations,
            rideObservations,
            historicalNames,
            ComputeFingerprint(visitDocuments, occurrenceDocuments),
            isStable);
    }

    private static ProjectionDefinition<UserVisitDocument, YearRecapVisitSourceDocument>
        BuildVisitProjection()
    {
        return Builders<UserVisitDocument>.Projection.Expression(
            static value => new YearRecapVisitSourceDocument
            {
                Id = value.Id,
                ParkId = value.ParkId,
                Date = value.Date,
                ParkAssessmentValueHalfSteps = value.ParkAssessment == null
                    ? null
                    : value.ParkAssessment.ValueHalfSteps,
                Version = value.Version,
                ContentMutationLeaseToken = value.ContentMutationLeaseToken,
                ContentMutationFenceToken = value.ContentMutationFenceToken,
                ContentMutationFenceStableToken = value.ContentMutationFenceStableToken,
                ContentMutationFenceReady = value.ContentMutationFenceReady,
            });
    }

    private static ProjectionDefinition<UserRideOccurrenceDocument, YearRecapRideSourceDocument>
        BuildOccurrenceProjection()
    {
        return Builders<UserRideOccurrenceDocument>.Projection.Expression(
            static value => new YearRecapRideSourceDocument
            {
                Id = value.Id,
                VisitId = value.VisitId,
                ParkId = value.ParkId,
                ParkItemId = value.ParkItemId,
                Status = value.Status,
                AssessmentValueHalfSteps = value.Assessment == null
                    ? null
                    : value.Assessment.ValueHalfSteps,
                HistoricalName = value.HistoricalTarget == null
                    ? null
                    : value.HistoricalTarget.Name,
                HistoricalCategory = value.HistoricalTarget == null
                    ? null
                    : value.HistoricalTarget.Category,
                Version = value.Version,
                ContentMutationFenceToken = value.ContentMutationFenceToken,
            });
    }

    private static bool IsStable(YearRecapVisitSourceDocument visit)
    {
        bool hasFence = visit.ContentMutationFenceToken.HasValue;
        return string.IsNullOrWhiteSpace(visit.ContentMutationLeaseToken)
            && (!hasFence
                || visit.ContentMutationFenceReady
                && visit.ContentMutationFenceStableToken == visit.ContentMutationFenceToken);
    }

    internal static string ComputeFingerprint(
        IReadOnlyCollection<YearRecapVisitSourceDocument> visits,
        IReadOnlyCollection<YearRecapRideSourceDocument> occurrences)
    {
        StringBuilder canonical = new StringBuilder();
        Append(canonical, YearRecapShareVersion.CalculationVersion);
        foreach (YearRecapVisitSourceDocument visit in visits.OrderBy(
                     static value => value.Id,
                     StringComparer.Ordinal))
        {
            Append(canonical, "visit");
            Append(canonical, visit.Id);
            Append(canonical, visit.ParkId);
            Append(canonical, visit.Date.Year);
            Append(canonical, visit.Date.Month);
            Append(canonical, visit.Date.Day);
            Append(canonical, visit.Date.Precision.ToString());
            Append(canonical, visit.Date.IsApproximate);
            Append(canonical, visit.ParkAssessmentValueHalfSteps);
            Append(canonical, visit.Version);
            Append(canonical, visit.ContentMutationFenceToken);
            Append(canonical, visit.ContentMutationFenceStableToken);
            Append(canonical, visit.ContentMutationFenceReady);
        }

        foreach (YearRecapRideSourceDocument occurrence in occurrences.OrderBy(
                     static value => value.Id,
                     StringComparer.Ordinal))
        {
            Append(canonical, "ride");
            Append(canonical, occurrence.Id);
            Append(canonical, occurrence.VisitId);
            Append(canonical, occurrence.ParkId);
            Append(canonical, occurrence.ParkItemId);
            Append(canonical, occurrence.Status.ToString());
            Append(canonical, occurrence.AssessmentValueHalfSteps);
            Append(canonical, occurrence.HistoricalName);
            Append(canonical, occurrence.HistoricalCategory);
            Append(canonical, occurrence.Version);
            Append(canonical, occurrence.ContentMutationFenceToken);
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(digest);
    }

    private static void Append(StringBuilder canonical, object? value)
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        canonical.Append(text.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(text);
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

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }
}
