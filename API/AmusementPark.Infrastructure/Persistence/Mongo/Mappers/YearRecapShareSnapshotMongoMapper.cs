using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class YearRecapShareSnapshotMongoMapper
{
    public static YearRecapShareSnapshotDocument ToDocument(
        this YearRecapShareSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new YearRecapShareSnapshotDocument
        {
            Id = CreateDocumentId(snapshot.PublicationId.Value, snapshot.PublicationVersion),
            PublicationId = snapshot.PublicationId.Value,
            PublicationVersion = snapshot.PublicationVersion,
            PublicationStateVersion = snapshot.PublicationStateVersion,
            SourceVersion = snapshot.SourceVersion,
            PolicySchemaVersion = snapshot.PolicySchemaVersion,
            DatePrecision = snapshot.DatePrecision,
            IncludedFields = snapshot.IncludedFields.ToList(),
            ContentFingerprint = snapshot.ContentFingerprint,
            Content = ToDocument(snapshot.Content),
            CreatedAt = snapshot.CreatedAtUtc,
            UpdatedAt = snapshot.CreatedAtUtc,
        };
    }

    public static YearRecapShareSnapshot ToDomain(
        this YearRecapShareSnapshotDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Content);
        return new YearRecapShareSnapshot(
            SharePublicationId.Parse(document.PublicationId),
            document.PublicationVersion,
            document.PublicationStateVersion,
            document.SourceVersion,
            document.PolicySchemaVersion,
            document.DatePrecision,
            document.IncludedFields,
            document.ContentFingerprint,
            ToResult(document.Content),
            document.CreatedAt);
    }

    public static string CreateDocumentId(string publicationId, long publicationVersion)
    {
        return string.Concat(publicationId, ":", publicationVersion);
    }

    private static YearRecapShareContentDocument ToDocument(YearRecapSharePreviewResult value)
    {
        return new YearRecapShareContentDocument
        {
            Year = value.Year,
            ParkCount = value.ParkCount,
            VisitCount = value.VisitCount,
            ApproximateVisitCount = value.ApproximateVisitCount,
            ApproximateVisitRate = value.ApproximateVisitRate,
            TotalRideCount = value.TotalRideCount,
            DistinctItemCount = value.DistinctItemCount,
            MissedItemCount = value.MissedItemCount,
            Categories = value.Categories.ToList(),
            ParkRatings = ToDocument(value.ParkRatings),
            RideRatings = ToDocument(value.RideRatings),
            MostVisitedParks = value.MostVisitedParks.Select(ToDocument).ToList(),
            MostRepeatedItem = ToDocument(value.MostRepeatedItem),
            TopRatedItem = ToDocument(value.TopRatedItem),
            RatingEvolution = ToDocument(value.RatingEvolution),
            NowClosedItems = value.NowClosedItems.Select(ToRequiredDocument).ToList(),
            PublicCaption = value.PublicCaption,
            HasIncompleteCatalog = value.HasIncompleteCatalog,
            CalculationVersion = value.CalculationVersion,
            IsEmpty = value.IsEmpty,
        };
    }

    private static YearRecapSharePreviewResult ToResult(YearRecapShareContentDocument value)
    {
        return new YearRecapSharePreviewResult(
            value.Year,
            value.ParkCount,
            value.VisitCount,
            value.ApproximateVisitCount,
            value.ApproximateVisitRate,
            value.TotalRideCount,
            value.DistinctItemCount,
            value.MissedItemCount,
            value.Categories,
            ToResult(value.ParkRatings),
            ToResult(value.RideRatings),
            value.MostVisitedParks.Select(ToResult).ToArray(),
            ToResult(value.MostRepeatedItem),
            ToResult(value.TopRatedItem),
            ToResult(value.RatingEvolution),
            value.NowClosedItems.Select(ToRequiredResult).ToArray(),
            value.PublicCaption,
            value.HasIncompleteCatalog,
            value.CalculationVersion,
            value.IsEmpty);
    }

    private static YearRecapShareRatingSummaryDocument? ToDocument(
        YearRecapShareRatingSummaryResult? value)
    {
        return value is null
            ? null
            : new YearRecapShareRatingSummaryDocument
            {
                RatedCount = value.RatedCount,
                EligibleCount = value.EligibleCount,
                Average = value.Average,
            };
    }

    private static YearRecapShareRatingSummaryResult? ToResult(
        YearRecapShareRatingSummaryDocument? value)
    {
        return value is null
            ? null
            : new YearRecapShareRatingSummaryResult(
                value.RatedCount,
                value.EligibleCount,
                value.Average);
    }

    private static YearRecapShareParkDocument ToDocument(YearRecapShareParkResult value)
    {
        return new YearRecapShareParkDocument
        {
            Name = value.Name,
            VisitCount = value.VisitCount,
            CompletedRideCount = value.CompletedRideCount,
        };
    }

    private static YearRecapShareParkResult ToResult(YearRecapShareParkDocument value)
    {
        return new YearRecapShareParkResult(
            value.Name,
            value.VisitCount,
            value.CompletedRideCount);
    }

    private static YearRecapShareHighlightDocument? ToDocument(
        YearRecapShareHighlightResult? value)
    {
        return value is null
            ? null
            : new YearRecapShareHighlightDocument
            {
                Name = value.Name,
                RideCount = value.RideCount,
                RatingCount = value.RatingCount,
                AverageRating = value.AverageRating,
                IsNowClosed = value.IsNowClosed,
            };
    }

    private static YearRecapShareHighlightDocument ToRequiredDocument(
        YearRecapShareHighlightResult value)
    {
        return ToDocument(value)!;
    }

    private static YearRecapShareHighlightResult? ToResult(
        YearRecapShareHighlightDocument? value)
    {
        return value is null
            ? null
            : new YearRecapShareHighlightResult(
                value.Name,
                value.RideCount,
                value.RatingCount,
                value.AverageRating,
                value.IsNowClosed);
    }

    private static YearRecapShareHighlightResult ToRequiredResult(
        YearRecapShareHighlightDocument value)
    {
        return ToResult(value)!;
    }

    private static YearRecapShareTrendDocument? ToDocument(YearRecapShareTrendResult? value)
    {
        return value is null
            ? null
            : new YearRecapShareTrendDocument
            {
                Name = value.Name,
                Kind = value.Kind,
                FirstWindowRatingCount = value.FirstWindowRatingCount,
                LastWindowRatingCount = value.LastWindowRatingCount,
                FirstWindowAverage = value.FirstWindowAverage,
                LastWindowAverage = value.LastWindowAverage,
                Delta = value.Delta,
            };
    }

    private static YearRecapShareTrendResult? ToResult(YearRecapShareTrendDocument? value)
    {
        return value is null
            ? null
            : new YearRecapShareTrendResult(
                value.Name,
                value.Kind,
                value.FirstWindowRatingCount,
                value.LastWindowRatingCount,
                value.FirstWindowAverage,
                value.LastWindowAverage,
                value.Delta);
    }
}
