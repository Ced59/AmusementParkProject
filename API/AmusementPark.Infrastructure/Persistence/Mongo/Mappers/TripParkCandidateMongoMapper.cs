using System.Globalization;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class TripParkCandidateMongoMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    public static TripParkCandidateDocument ToDocument(this TripParkCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return new TripParkCandidateDocument
        {
            Id = candidate.Id.Value,
            TripPlanId = candidate.TripPlanId.Value,
            ParkId = candidate.ParkId,
            CandidateDates = candidate.CandidateDates
                .Select(static date => date.ToString(DateFormat, CultureInfo.InvariantCulture))
                .ToList(),
            Source = candidate.Source,
            CandidateState = candidate.State,
            CollectiveNote = candidate.CollectiveNote,
            FitSnapshot = candidate.FitSnapshot?.ToDocument(),
            AddedByMemberId = candidate.AddedByMemberId.Value,
            SortPosition = candidate.SortPosition,
            Version = candidate.Version,
            CreatedAt = ToMongoPrecision(candidate.CreatedAtUtc),
            UpdatedAt = ToMongoPrecision(candidate.UpdatedAtUtc),
        };
    }

    public static TripParkCandidate ToDomain(this TripParkCandidateDocument document)
    {
        return ToDomain(document, document.SortPosition);
    }

    public static TripParkCandidate ToDomain(
        this TripParkCandidateDocument document,
        long sortPosition)
    {
        ArgumentNullException.ThrowIfNull(document);
        return TripParkCandidate.Restore(
            TripParkCandidateId.Parse(document.Id),
            TripPlanId.Parse(document.TripPlanId),
            document.ParkId,
            document.CandidateDates.Select(ParseDate).ToArray(),
            document.Source,
            document.CandidateState,
            document.CollectiveNote,
            document.FitSnapshot?.ToDomain(),
            TripMemberId.Parse(document.AddedByMemberId),
            sortPosition,
            document.Version,
            DateTime.SpecifyKind(document.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(document.UpdatedAt, DateTimeKind.Utc));
    }

    private static TripFitRecommendationSnapshotDocument ToDocument(
        this TripFitRecommendationSnapshot snapshot)
    {
        return new TripFitRecommendationSnapshotDocument
        {
            MethodVersion = snapshot.MethodVersion,
            Explanation = snapshot.Explanation,
            CalculatedAtUtc = ToMongoPrecision(snapshot.CalculatedAtUtc),
        };
    }

    private static TripFitRecommendationSnapshot ToDomain(
        this TripFitRecommendationSnapshotDocument document)
    {
        return new TripFitRecommendationSnapshot(
            document.MethodVersion,
            document.Explanation,
            DateTime.SpecifyKind(document.CalculatedAtUtc, DateTimeKind.Utc));
    }

    private static DateOnly ParseDate(string value)
    {
        return DateOnly.ParseExact(value, DateFormat, CultureInfo.InvariantCulture);
    }

    private static DateTime ToMongoPrecision(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
