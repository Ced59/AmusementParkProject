using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportShareSnapshotExportWriter
{
    public static IReadOnlyCollection<string> CsvFileNames { get; } = new[]
    {
        "share-snapshots.csv",
        "passport-share-selections.csv",
    };

    public static void WriteJson(
        Utf8JsonWriter writer,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        writer.WriteStartArray("shareSnapshots");
        foreach (VisitRecapShareSnapshot snapshot in request.ShareLifecycle.VisitSnapshots)
        {
            WriteSnapshot(
                writer,
                references,
                snapshot.PublicationId,
                "VisitRecap",
                snapshot.PublicationVersion,
                snapshot.PublicationStateVersion,
                snapshot.SourceVersion,
                snapshot.PolicySchemaVersion,
                snapshot.DatePrecision,
                snapshot.IncludedFields,
                snapshot.Content.PublicCaption,
                snapshot.CreatedAtUtc);
        }

        foreach (YearRecapShareSnapshot snapshot in request.ShareLifecycle.YearSnapshots)
        {
            WriteSnapshot(
                writer,
                references,
                snapshot.PublicationId,
                "YearRecap",
                snapshot.PublicationVersion,
                snapshot.PublicationStateVersion,
                snapshot.SourceVersion,
                snapshot.PolicySchemaVersion,
                snapshot.DatePrecision,
                snapshot.IncludedFields,
                snapshot.Content.PublicCaption,
                snapshot.CreatedAtUtc);
        }

        foreach (PassportProfileShareSnapshot snapshot in request.ShareLifecycle.PassportSnapshots)
        {
            WritePassportSnapshot(writer, references, snapshot, request);
        }

        writer.WriteEndArray();
    }

    public static void WriteCsvEntries(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        WriteSnapshotsCsv(archive, request.ShareLifecycle, references);
        WritePassportSelectionsCsv(
            archive,
            request.ShareLifecycle.PassportSnapshots,
            request,
            references);
    }

    private static void WriteSnapshot(
        Utf8JsonWriter writer,
        PassportExportReferenceMap references,
        SharePublicationId publicationId,
        string type,
        long publicationVersion,
        long publicationStateVersion,
        long sourceVersion,
        int policySchemaVersion,
        ShareDatePrecision datePrecision,
        IEnumerable<ShareContentField> includedFields,
        string? publicCaption,
        DateTime createdAtUtc)
    {
        writer.WriteStartObject();
        WriteSnapshotMetadata(
            writer,
            references,
            publicationId,
            type,
            publicationVersion,
            publicationStateVersion,
            sourceVersion,
            policySchemaVersion,
            datePrecision,
            includedFields,
            publicCaption,
            createdAtUtc);
        writer.WriteEndObject();
    }

    private static void WritePassportSnapshot(
        Utf8JsonWriter writer,
        PassportExportReferenceMap references,
        PassportProfileShareSnapshot snapshot,
        PassportExportWriteRequest request)
    {
        writer.WriteStartObject();
        WriteSnapshotMetadata(
            writer,
            references,
            snapshot.PublicationId,
            "PassportProfile",
            snapshot.PublicationVersion,
            snapshot.PublicationStateVersion,
            snapshot.SourceVersion,
            snapshot.PolicySchemaVersion,
            snapshot.DatePrecision,
            snapshot.IncludedFields,
            snapshot.Selection.PublicCaption,
            snapshot.CreatedAtUtc);
        writer.WriteStartObject("selection");
        writer.WriteStartArray("years");
        foreach (int year in snapshot.Selection.SelectedYears ?? Array.Empty<int>())
        {
            writer.WriteNumberValue(year);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("parks");
        foreach ((string Name, string? CountryCode) park in ResolveSelectedParks(snapshot, request))
        {
            writer.WriteStartObject();
            writer.WriteString("name", park.Name);
            WriteNullableString(writer, "countryCode", park.CountryCode);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("ratings");
        foreach (PassportProfileShareRatingResult rating in snapshot.Content.PersonalRanking)
        {
            writer.WriteStartObject();
            writer.WriteString("targetType", rating.TargetType);
            writer.WriteString("name", rating.Name);
            WriteNullableString(writer, "parkName", rating.ParkName);
            WriteNullableString(writer, "category", rating.Category);
            writer.WriteNumber("rating", rating.Rating);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteString("visibility", snapshot.Selection.Visibility.ToString());
        writer.WriteBoolean("allowsComparisons", snapshot.Selection.AllowsComparisons);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteSnapshotMetadata(
        Utf8JsonWriter writer,
        PassportExportReferenceMap references,
        SharePublicationId publicationId,
        string type,
        long publicationVersion,
        long publicationStateVersion,
        long sourceVersion,
        int policySchemaVersion,
        ShareDatePrecision datePrecision,
        IEnumerable<ShareContentField> includedFields,
        string? publicCaption,
        DateTime createdAtUtc)
    {
        writer.WriteString("publicationReference", references.Publication(publicationId));
        writer.WriteString("type", type);
        writer.WriteNumber("publicationVersion", publicationVersion);
        writer.WriteNumber("publicationStateVersion", publicationStateVersion);
        writer.WriteNumber("sourceVersion", sourceVersion);
        writer.WriteNumber("policySchemaVersion", policySchemaVersion);
        writer.WriteString("datePrecision", datePrecision.ToString());
        writer.WriteStartArray("includedFields");
        foreach (ShareContentField field in includedFields)
        {
            writer.WriteStringValue(field.ToString());
        }

        writer.WriteEndArray();
        WriteNullableString(writer, "publicCaption", publicCaption);
        writer.WriteString("createdAtUtc", FormatUtc(createdAtUtc));
    }

    private static void WriteSnapshotsCsv(
        ZipArchive archive,
        PassportShareLifecycleExportData lifecycle,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "share-snapshots.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "publicationReference", "type", "publicationVersion", "publicationStateVersion",
            "sourceVersion", "policySchemaVersion", "datePrecision", "includedFields",
            "publicCaption", "createdAtUtc",
        });
        foreach (VisitRecapShareSnapshot snapshot in lifecycle.VisitSnapshots)
        {
            WriteSnapshotCsvRow(
                writer,
                references,
                snapshot.PublicationId,
                "VisitRecap",
                snapshot.PublicationVersion,
                snapshot.PublicationStateVersion,
                snapshot.SourceVersion,
                snapshot.PolicySchemaVersion,
                snapshot.DatePrecision,
                snapshot.IncludedFields,
                snapshot.Content.PublicCaption,
                snapshot.CreatedAtUtc);
        }

        foreach (YearRecapShareSnapshot snapshot in lifecycle.YearSnapshots)
        {
            WriteSnapshotCsvRow(
                writer,
                references,
                snapshot.PublicationId,
                "YearRecap",
                snapshot.PublicationVersion,
                snapshot.PublicationStateVersion,
                snapshot.SourceVersion,
                snapshot.PolicySchemaVersion,
                snapshot.DatePrecision,
                snapshot.IncludedFields,
                snapshot.Content.PublicCaption,
                snapshot.CreatedAtUtc);
        }

        foreach (PassportProfileShareSnapshot snapshot in lifecycle.PassportSnapshots)
        {
            WriteSnapshotCsvRow(
                writer,
                references,
                snapshot.PublicationId,
                "PassportProfile",
                snapshot.PublicationVersion,
                snapshot.PublicationStateVersion,
                snapshot.SourceVersion,
                snapshot.PolicySchemaVersion,
                snapshot.DatePrecision,
                snapshot.IncludedFields,
                snapshot.Selection.PublicCaption,
                snapshot.CreatedAtUtc);
        }
    }

    private static void WriteSnapshotCsvRow(
        StreamWriter writer,
        PassportExportReferenceMap references,
        SharePublicationId publicationId,
        string type,
        long publicationVersion,
        long publicationStateVersion,
        long sourceVersion,
        int policySchemaVersion,
        ShareDatePrecision datePrecision,
        IEnumerable<ShareContentField> includedFields,
        string? publicCaption,
        DateTime createdAtUtc)
    {
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            references.Publication(publicationId),
            type,
            PassportShareLifecycleExportWriter.Integer(publicationVersion),
            PassportShareLifecycleExportWriter.Integer(publicationStateVersion),
            PassportShareLifecycleExportWriter.Integer(sourceVersion),
            PassportShareLifecycleExportWriter.Integer(policySchemaVersion),
            datePrecision.ToString(),
            Join(includedFields),
            publicCaption,
            FormatUtc(createdAtUtc),
        });
    }

    private static void WritePassportSelectionsCsv(
        ZipArchive archive,
        IEnumerable<PassportProfileShareSnapshot> snapshots,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "passport-share-selections.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "publicationReference", "selectionType", "targetType", "year", "name",
            "parkName", "countryCode", "category", "rating", "visibility",
            "allowsComparisons",
        });
        foreach (PassportProfileShareSnapshot snapshot in snapshots)
        {
            WritePassportSelectionRows(writer, references, snapshot, request);
        }
    }

    private static void WritePassportSelectionRows(
        StreamWriter writer,
        PassportExportReferenceMap references,
        PassportProfileShareSnapshot snapshot,
        PassportExportWriteRequest request)
    {
        string publicationReference = references.Publication(snapshot.PublicationId);
        string visibility = snapshot.Selection.Visibility.ToString();
        string allowsComparisons = snapshot.Selection.AllowsComparisons ? "true" : "false";
        foreach (int year in snapshot.Selection.SelectedYears ?? Array.Empty<int>())
        {
            PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
            {
                publicationReference, "Year", null,
                year.ToString(CultureInfo.InvariantCulture), null, null, null, null, null,
                visibility, allowsComparisons,
            });
        }

        foreach ((string Name, string? CountryCode) park in ResolveSelectedParks(snapshot, request))
        {
            PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
            {
                publicationReference, "Park", null, null, park.Name, null,
                park.CountryCode, null, null, visibility, allowsComparisons,
            });
        }

        foreach (PassportProfileShareRatingResult rating in snapshot.Content.PersonalRanking)
        {
            PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
            {
                publicationReference, "Rating", rating.TargetType, null, rating.Name,
                rating.ParkName, null, rating.Category,
                PassportShareLifecycleExportWriter.Double(rating.Rating), visibility,
                allowsComparisons,
            });
        }
    }

    private static IEnumerable<(string Name, string? CountryCode)> ResolveSelectedParks(
        PassportProfileShareSnapshot snapshot,
        PassportExportWriteRequest request)
    {
        HashSet<string> selectedParkIds = new HashSet<string>(
            snapshot.Selection.SelectedParkIds ?? Array.Empty<string>(),
            StringComparer.Ordinal);
        HashSet<int> selectedYears = new HashSet<int>(
            snapshot.Selection.SelectedYears ?? Array.Empty<int>());
        HashSet<string> parksRepresentedByFrozenStatistics = snapshot.Content.Parks.Count == 0
            ? new HashSet<string>(StringComparer.Ordinal)
            : request.Visits
                .Where(visit => selectedYears.Contains(visit.Date.Year)
                    && selectedParkIds.Contains(visit.ParkId))
                .Select(static visit => visit.ParkId)
                .ToHashSet(StringComparer.Ordinal);
        if (snapshot.Content.Parks.Count > 0)
        {
            foreach (PassportProfileShareParkResult park in snapshot.Content.Parks)
            {
                string name = string.IsNullOrWhiteSpace(park.Name)
                    ? "Unavailable park"
                    : park.Name.Trim();
                yield return (name, NormalizeOptional(park.CountryCode));
            }

        }

        foreach (string parkId in snapshot.Selection.SelectedParkIds ?? Array.Empty<string>())
        {
            if (parksRepresentedByFrozenStatistics.Contains(parkId))
            {
                continue;
            }

            if (request.Parks.TryGetValue(parkId, out Park? park)
                && !string.IsNullOrWhiteSpace(park.Name))
            {
                yield return (park.Name.Trim(), NormalizeOptional(park.CountryCode));
            }
            else
            {
                yield return ("Unavailable park", null);
            }
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FormatUtc(DateTime value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Join<T>(IEnumerable<T> values)
    {
        return string.Join("|", values.Select(static value => value?.ToString()));
    }

    private static void WriteNullableString(
        Utf8JsonWriter writer,
        string propertyName,
        string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value);
        }
    }
}
