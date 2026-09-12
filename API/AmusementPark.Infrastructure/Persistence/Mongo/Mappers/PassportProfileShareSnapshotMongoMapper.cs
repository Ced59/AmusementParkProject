using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class PassportProfileShareSnapshotMongoMapper
{
    public static PassportProfileShareSnapshotDocument ToDocument(
        this PassportProfileShareSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new PassportProfileShareSnapshotDocument
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
            Selection = ToDocument(snapshot.Selection),
            Content = ToDocument(snapshot.Content),
            CreatedAt = snapshot.CreatedAtUtc,
            UpdatedAt = snapshot.CreatedAtUtc,
        };
    }

    public static PassportProfileShareSnapshot ToDomain(
        this PassportProfileShareSnapshotDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Selection);
        ArgumentNullException.ThrowIfNull(document.Content);
        return new PassportProfileShareSnapshot(
            SharePublicationId.Parse(document.PublicationId),
            document.PublicationVersion,
            document.PublicationStateVersion,
            document.SourceVersion,
            document.PolicySchemaVersion,
            document.DatePrecision,
            document.IncludedFields,
            document.ContentFingerprint,
            ToInput(document.Selection),
            ToResult(document.Content),
            document.CreatedAt);
    }

    public static string CreateDocumentId(string publicationId, long publicationVersion)
    {
        return string.Concat(publicationId, ":", publicationVersion);
    }

    private static PassportProfileShareSelectionDocument ToDocument(
        PassportProfileShareInput value)
    {
        return new PassportProfileShareSelectionDocument
        {
            SelectedYears = (value.SelectedYears ?? Array.Empty<int>()).ToList(),
            SelectedParkIds = (value.SelectedParkIds ?? Array.Empty<string>()).ToList(),
            SelectedRatingKeys = (value.SelectedRatingKeys ?? Array.Empty<string>()).ToList(),
            PublicCaption = value.PublicCaption,
            Visibility = value.Visibility,
            AllowsComparisons = value.AllowsComparisons,
        };
    }

    private static PassportProfileShareInput ToInput(
        PassportProfileShareSelectionDocument value)
    {
        return new PassportProfileShareInput(
            value.SelectedYears,
            value.SelectedParkIds,
            value.SelectedRatingKeys,
            value.PublicCaption,
            value.Visibility,
            value.AllowsComparisons);
    }

    private static PassportProfileShareContentDocument ToDocument(
        PassportProfileSharePreviewResult value)
    {
        return new PassportProfileShareContentDocument
        {
            DisplayName = value.DisplayName,
            AvatarUrl = value.AvatarUrl,
            PublicCaption = value.PublicCaption,
            Visibility = value.Visibility,
            AllowsComparisons = value.AllowsComparisons,
            ParkCount = value.ParkCount,
            VisitCount = value.VisitCount,
            TotalRideCount = value.TotalRideCount,
            DistinctItemCount = value.DistinctItemCount,
            VisitRatings = ToDocument(value.VisitRatings),
            RideRatings = ToDocument(value.RideRatings),
            Countries = value.Countries.Select(ToDocument).ToList(),
            Years = value.Years.Select(ToDocument).ToList(),
            Parks = value.Parks.Select(ToDocument).ToList(),
            PersonalRanking = value.PersonalRanking.Select(ToDocument).ToList(),
            MissedItems = value.MissedItems.Select(ToDocument).ToList(),
            HasIncompleteCatalog = value.HasIncompleteCatalog,
            CalculationVersion = value.CalculationVersion,
            IsEmpty = value.IsEmpty,
        };
    }

    private static PassportProfileSharePreviewResult ToResult(
        PassportProfileShareContentDocument value)
    {
        return new PassportProfileSharePreviewResult(
            value.DisplayName,
            value.AvatarUrl,
            value.PublicCaption,
            value.Visibility,
            value.AllowsComparisons,
            value.ParkCount,
            value.VisitCount,
            value.TotalRideCount,
            value.DistinctItemCount,
            ToResult(value.VisitRatings),
            ToResult(value.RideRatings),
            value.Countries.Select(ToResult).ToArray(),
            value.Years.Select(ToResult).ToArray(),
            value.Parks.Select(ToResult).ToArray(),
            value.PersonalRanking.Select(ToResult).ToArray(),
            value.MissedItems.Select(ToResult).ToArray(),
            value.HasIncompleteCatalog,
            value.CalculationVersion,
            value.IsEmpty);
    }

    private static PassportProfileShareRatingSummaryDocument? ToDocument(
        PassportProfileShareRatingSummaryResult? value)
    {
        return value is null
            ? null
            : new PassportProfileShareRatingSummaryDocument
            {
                RatedCount = value.RatedCount,
                EligibleCount = value.EligibleCount,
                Average = value.Average,
            };
    }

    private static PassportProfileShareRatingSummaryResult? ToResult(
        PassportProfileShareRatingSummaryDocument? value)
    {
        return value is null
            ? null
            : new PassportProfileShareRatingSummaryResult(
                value.RatedCount,
                value.EligibleCount,
                value.Average);
    }

    private static PassportProfileShareCountryDocument ToDocument(
        PassportProfileShareCountryResult value)
    {
        return new PassportProfileShareCountryDocument
        {
            CountryCode = value.CountryCode,
            ParkCount = value.ParkCount,
            VisitCount = value.VisitCount,
        };
    }

    private static PassportProfileShareCountryResult ToResult(
        PassportProfileShareCountryDocument value)
    {
        return new PassportProfileShareCountryResult(
            value.CountryCode,
            value.ParkCount,
            value.VisitCount);
    }

    private static PassportProfileShareYearDocument ToDocument(
        PassportProfileShareYearResult value)
    {
        return new PassportProfileShareYearDocument
        {
            Year = value.Year,
            VisitCount = value.VisitCount,
            ParkCount = value.ParkCount,
            CompletedRideCount = value.CompletedRideCount,
        };
    }

    private static PassportProfileShareYearResult ToResult(
        PassportProfileShareYearDocument value)
    {
        return new PassportProfileShareYearResult(
            value.Year,
            value.VisitCount,
            value.ParkCount,
            value.CompletedRideCount);
    }

    private static PassportProfileShareParkDocument ToDocument(
        PassportProfileShareParkResult value)
    {
        return new PassportProfileShareParkDocument
        {
            Name = value.Name,
            CountryCode = value.CountryCode,
            VisitCount = value.VisitCount,
            FirstVisitYear = value.FirstVisitYear,
            LastVisitYear = value.LastVisitYear,
            CompletedRideCount = value.CompletedRideCount,
            VisitRatings = ToDocument(value.VisitRatings),
        };
    }

    private static PassportProfileShareParkResult ToResult(
        PassportProfileShareParkDocument value)
    {
        return new PassportProfileShareParkResult(
            value.Name,
            value.CountryCode,
            value.VisitCount,
            value.FirstVisitYear,
            value.LastVisitYear,
            value.CompletedRideCount,
            ToResult(value.VisitRatings));
    }

    private static PassportProfileShareRatingDocument ToDocument(
        PassportProfileShareRatingResult value)
    {
        return new PassportProfileShareRatingDocument
        {
            TargetType = value.TargetType,
            Name = value.Name,
            ParkName = value.ParkName,
            Category = value.Category,
            Rating = value.Rating,
        };
    }

    private static PassportProfileShareRatingResult ToResult(
        PassportProfileShareRatingDocument value)
    {
        return new PassportProfileShareRatingResult(
            value.TargetType,
            value.Name,
            value.ParkName,
            value.Category,
            value.Rating);
    }

    private static PassportProfileShareMissedItemDocument ToDocument(
        PassportProfileShareMissedItemResult value)
    {
        return new PassportProfileShareMissedItemDocument
        {
            Name = value.Name,
            Status = value.Status,
            OccurrenceCount = value.OccurrenceCount,
        };
    }

    private static PassportProfileShareMissedItemResult ToResult(
        PassportProfileShareMissedItemDocument value)
    {
        return new PassportProfileShareMissedItemResult(
            value.Name,
            value.Status,
            value.OccurrenceCount);
    }
}
