using System.IO.Compression;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportShareLifecycleComparisonCsvWriter
{
    public static void WriteCsvEntries(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        WriteParks(archive, request, references);
        WriteRatings(archive, request, references);
        WriteYears(archive, request, references);
        WriteMissedItems(archive, request, references);
    }

    private static void WriteParks(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "comparison-parks.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "comparisonReference", "name", "countryCode", "yourVisitCount",
            "otherMemberVisitCount",
        });
        foreach (ProfileComparison comparison in request.ShareLifecycle.Comparisons)
        {
            bool isCreator = PassportShareLifecycleExportWriter.IsCreator(
                request.UserId,
                comparison.CreatorUserId);
            foreach (ProfileComparisonParkResult park in comparison.Calculation.Parks)
            {
                PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
                {
                    references.Comparison(comparison.Id), park.Name, park.CountryCode,
                    PassportShareLifecycleExportWriter.NullableInteger(
                        isCreator ? park.CreatorVisitCount : park.AcceptorVisitCount),
                    PassportShareLifecycleExportWriter.NullableInteger(
                        isCreator ? park.AcceptorVisitCount : park.CreatorVisitCount),
                });
            }
        }
    }

    private static void WriteRatings(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "comparison-ratings.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "comparisonReference", "targetType", "name", "parkName", "category",
            "yourRating", "otherMemberRating", "absoluteDifference", "affinity",
        });
        foreach (ProfileComparison comparison in request.ShareLifecycle.Comparisons)
        {
            bool isCreator = PassportShareLifecycleExportWriter.IsCreator(
                request.UserId,
                comparison.CreatorUserId);
            foreach (ProfileComparisonRatingResult rating in comparison.Calculation.Ratings)
            {
                PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
                {
                    references.Comparison(comparison.Id), rating.TargetType, rating.Name,
                    rating.ParkName, rating.Category,
                    PassportShareLifecycleExportWriter.Double(
                        isCreator ? rating.CreatorRating : rating.AcceptorRating),
                    PassportShareLifecycleExportWriter.Double(
                        isCreator ? rating.AcceptorRating : rating.CreatorRating),
                    PassportShareLifecycleExportWriter.Double(rating.AbsoluteDifference),
                    rating.Affinity.ToString(),
                });
            }
        }
    }

    private static void WriteYears(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "comparison-years.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "comparisonReference", "year", "yourVisitCount", "otherMemberVisitCount",
            "yourRideCount", "otherMemberRideCount",
        });
        foreach (ProfileComparison comparison in request.ShareLifecycle.Comparisons)
        {
            bool isCreator = PassportShareLifecycleExportWriter.IsCreator(
                request.UserId,
                comparison.CreatorUserId);
            foreach (ProfileComparisonYearResult year in comparison.Calculation.Years)
            {
                PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
                {
                    references.Comparison(comparison.Id),
                    PassportShareLifecycleExportWriter.Integer(year.Year),
                    PassportShareLifecycleExportWriter.Integer(
                        isCreator ? year.CreatorVisitCount : year.AcceptorVisitCount),
                    PassportShareLifecycleExportWriter.Integer(
                        isCreator ? year.AcceptorVisitCount : year.CreatorVisitCount),
                    PassportShareLifecycleExportWriter.NullableInteger(
                        isCreator ? year.CreatorRideCount : year.AcceptorRideCount),
                    PassportShareLifecycleExportWriter.NullableInteger(
                        isCreator ? year.AcceptorRideCount : year.CreatorRideCount),
                });
            }
        }
    }

    private static void WriteMissedItems(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "comparison-missed-items.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "comparisonReference", "name", "status", "yourOccurrenceCount",
            "otherMemberOccurrenceCount",
        });
        foreach (ProfileComparison comparison in request.ShareLifecycle.Comparisons)
        {
            bool isCreator = PassportShareLifecycleExportWriter.IsCreator(
                request.UserId,
                comparison.CreatorUserId);
            foreach (ProfileComparisonMissedItemResult item in comparison.Calculation.MissedItems)
            {
                PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
                {
                    references.Comparison(comparison.Id), item.Name, item.Status,
                    PassportShareLifecycleExportWriter.NullableInteger(isCreator
                        ? item.CreatorOccurrenceCount
                        : item.AcceptorOccurrenceCount),
                    PassportShareLifecycleExportWriter.NullableInteger(isCreator
                        ? item.AcceptorOccurrenceCount
                        : item.CreatorOccurrenceCount),
                });
            }
        }
    }
}
