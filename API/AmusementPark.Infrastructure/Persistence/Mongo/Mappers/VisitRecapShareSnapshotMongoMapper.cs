using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class VisitRecapShareSnapshotMongoMapper
{
    public static VisitRecapShareSnapshotDocument ToDocument(
        this VisitRecapShareSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new VisitRecapShareSnapshotDocument
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

    public static VisitRecapShareSnapshot ToDomain(
        this VisitRecapShareSnapshotDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Content);
        return new VisitRecapShareSnapshot(
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

    private static VisitRecapShareContentDocument ToDocument(
        VisitRecapSharePreviewResult content)
    {
        return new VisitRecapShareContentDocument
        {
            ParkId = content.ParkId,
            ParkName = content.ParkName,
            Date = content.Date is null
                ? null
                : new VisitRecapShareDateDocument
                {
                    Year = content.Date.Year,
                    Month = content.Date.Month,
                    Day = content.Date.Day,
                    Precision = content.Date.Precision,
                    IsApproximate = content.Date.IsApproximate,
                },
            DistinctItemCount = content.DistinctItemCount,
            TotalRideCount = content.TotalRideCount,
            Categories = content.Categories.ToList(),
            ParkRating = content.ParkRating,
            TopRatedItem = ToDocument(content.TopRatedItem),
            MostRepeatedItem = ToDocument(content.MostRepeatedItem),
            Items = content.Items.Select(ToDocument).ToList(),
            PublicCaption = content.PublicCaption,
            HasHiddenDate = content.HasHiddenDate,
            HasIncompleteRatings = content.HasIncompleteRatings,
            HasIncompleteItems = content.HasIncompleteItems,
        };
    }

    private static VisitRecapShareHighlightDocument? ToDocument(
        VisitRecapShareHighlightResult? value)
    {
        return value is null
            ? null
            : new VisitRecapShareHighlightDocument
            {
                Name = value.Name,
                RideCount = value.RideCount,
                Rating = value.Rating,
            };
    }

    private static VisitRecapShareItemDocument ToDocument(
        VisitRecapShareItemResult value)
    {
        return new VisitRecapShareItemDocument
        {
            ParkItemId = value.ParkItemId,
            Name = value.Name,
            Category = value.Category,
            RideCount = value.RideCount,
            AverageRating = value.AverageRating,
            IsMissed = value.IsMissed,
        };
    }

    private static VisitRecapSharePreviewResult ToResult(
        VisitRecapShareContentDocument content)
    {
        return new VisitRecapSharePreviewResult(
            content.ParkId,
            content.ParkName,
            content.Date is null
                ? null
                : new VisitRecapShareDateResult(
                    content.Date.Year,
                    content.Date.Month,
                    content.Date.Day,
                    content.Date.Precision,
                    content.Date.IsApproximate),
            content.DistinctItemCount,
            content.TotalRideCount,
            content.Categories,
            content.ParkRating,
            ToResult(content.TopRatedItem),
            ToResult(content.MostRepeatedItem),
            content.Items.Select(ToResult).ToArray(),
            content.PublicCaption,
            content.HasHiddenDate,
            content.HasIncompleteRatings,
            content.HasIncompleteItems);
    }

    private static VisitRecapShareHighlightResult? ToResult(
        VisitRecapShareHighlightDocument? value)
    {
        return value is null
            ? null
            : new VisitRecapShareHighlightResult(
                value.Name,
                value.RideCount,
                value.Rating);
    }

    private static VisitRecapShareItemResult ToResult(VisitRecapShareItemDocument value)
    {
        return new VisitRecapShareItemResult(
            value.ParkItemId,
            value.Name,
            value.Category,
            value.RideCount,
            value.AverageRating,
            value.IsMissed);
    }
}
