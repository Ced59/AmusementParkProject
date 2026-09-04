using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class CanonicalVisitExportWriter : IVisitExportWriter
{
    public const int SchemaVersion = 1;
    public const int MaximumArtifactBytes = 64 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

    public PassportExportArtifact Write(PassportExportWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        byte[] content = request.Format switch
        {
            PassportExportFormat.Json => WriteJson(request),
            PassportExportFormat.Csv => WriteCsvArchive(request),
            _ => throw new ArgumentOutOfRangeException(nameof(request)),
        };
        if (content.Length > MaximumArtifactBytes)
        {
            throw new PassportExportSizeLimitException();
        }

        string extension = request.Format == PassportExportFormat.Json ? "json" : "zip";
        string contentType = request.Format == PassportExportFormat.Json
            ? "application/json; charset=utf-8"
            : "application/zip";
        string date = request.ExportedAtUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string shortId = request.ExportId[..Math.Min(8, request.ExportId.Length)];
        return new PassportExportArtifact(
            $"amusement-park-passport-{date}-{shortId}.{extension}",
            contentType,
            content,
            SchemaVersion,
            Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant());
    }

    private static byte[] WriteJson(PassportExportWriteRequest request)
    {
        using MemoryStream output = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(output, new JsonWriterOptions
        {
            Indented = true,
        });
        writer.WriteStartObject();
        WriteSchema(writer, request, "json");
        writer.WriteStartArray("visits");
        foreach (Visit visit in request.Visits)
        {
            WriteVisit(writer, visit, request.Parks);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("rideOccurrences");
        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            WriteOccurrence(writer, occurrence, request.Parks, request.ParkItems);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("visitAssessments");
        foreach (Visit visit in request.Visits.Where(static visit => visit.ParkAssessment is not null))
        {
            WriteVisitAssessment(writer, visit, request.Parks);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("rideAssessments");
        foreach (RideOccurrence occurrence in request.RideOccurrences.Where(
                     static occurrence => occurrence.Assessment is not null))
        {
            WriteRideAssessment(writer, occurrence, request.ParkItems);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
        return output.ToArray();
    }

    private static byte[] WriteCsvArchive(PassportExportWriteRequest request)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteSchemaEntry(archive, request);
            WriteVisitsCsv(archive, request);
            WriteOccurrencesCsv(archive, request);
            WriteVisitAssessmentsCsv(archive, request);
            WriteRideAssessmentsCsv(archive, request);
        }

        return output.ToArray();
    }

    private static void WriteSchema(Utf8JsonWriter writer, PassportExportWriteRequest request, string format)
    {
        writer.WriteStartObject("schema");
        writer.WriteString("name", "amusement-park-passport");
        writer.WriteNumber("version", SchemaVersion);
        writer.WriteString("format", format);
        writer.WriteString("exportId", request.ExportId);
        writer.WriteString("exportedAtUtc", FormatUtc(request.ExportedAtUtc));
        writer.WriteString("datePolicy", "local-calendar-values-with-declared-precision");
        writer.WriteString("ratingScale", "half-steps-1-to-10-equivalent-to-0.5-to-5");
        writer.WriteEndObject();
    }

    private static void WriteSchemaEntry(ZipArchive archive, PassportExportWriteRequest request)
    {
        ZipArchiveEntry entry = archive.CreateEntry("schema.json", CompressionLevel.Optimal);
        using Stream stream = entry.Open();
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        WriteSchema(writer, request, "csv-zip");
        writer.WriteStartArray("files");
        writer.WriteStringValue("visits.csv");
        writer.WriteStringValue("ride-occurrences.csv");
        writer.WriteStringValue("visit-assessments.csv");
        writer.WriteStringValue("ride-assessments.csv");
        writer.WriteEndArray();
        writer.WriteString("encoding", "utf-8");
        writer.WriteString("delimiter", ",");
        writer.WriteEndObject();
    }

    private static void WriteVisit(
        Utf8JsonWriter writer,
        Visit visit,
        IReadOnlyDictionary<string, Park> parks)
    {
        ResolvePark(parks, visit.ParkId, out string parkName, out string parkStatus);
        writer.WriteStartObject();
        writer.WriteString("id", visit.Id.Value);
        writer.WriteString("parkId", visit.ParkId);
        writer.WriteString("parkName", parkName);
        writer.WriteString("parkStatus", parkStatus);
        writer.WriteNumber("year", visit.Date.Year);
        WriteNullableNumber(writer, "month", visit.Date.Month);
        WriteNullableNumber(writer, "day", visit.Date.Day);
        writer.WriteString("datePrecision", visit.Date.Precision.ToString());
        writer.WriteBoolean("dateIsApproximate", visit.Date.IsApproximate);
        WriteNullableString(writer, "timeZoneId", visit.TimeZoneId);
        writer.WriteString("serviceDayConvention", visit.ServiceDayConvention.ToString());
        writer.WriteString("status", visit.Status.ToString());
        writer.WriteString("privacy", visit.Privacy.ToString());
        WriteNullableString(writer, "title", visit.Title);
        WriteNullableString(writer, "privateNote", visit.PrivateNote);
        writer.WriteNumber("version", visit.Version);
        writer.WriteString("createdAtUtc", FormatUtc(visit.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(visit.UpdatedAtUtc));
        WriteNullableTimestamp(writer, "completedAtUtc", visit.CompletedAtUtc);
        writer.WriteEndObject();
    }

    private static void WriteOccurrence(
        Utf8JsonWriter writer,
        RideOccurrence occurrence,
        IReadOnlyDictionary<string, Park> parks,
        IReadOnlyDictionary<string, VisitTarget> parkItems)
    {
        ResolvePark(parks, occurrence.ParkId, out string parkName, out string parkStatus);
        ResolveParkItem(parkItems, occurrence, out string itemName, out string itemCategory, out string itemStatus);
        writer.WriteStartObject();
        writer.WriteString("id", occurrence.Id.Value);
        writer.WriteString("visitId", occurrence.VisitId.Value);
        writer.WriteString("parkId", occurrence.ParkId);
        writer.WriteString("parkName", parkName);
        writer.WriteString("parkStatus", parkStatus);
        writer.WriteString("parkItemId", occurrence.ParkItemId);
        writer.WriteString("parkItemName", itemName);
        writer.WriteString("parkItemCategory", itemCategory);
        writer.WriteString("parkItemStatus", itemStatus);
        writer.WriteNumber("sortPosition", occurrence.SortPosition);
        WriteNullableString(
            writer,
            "localTime",
            occurrence.Moment.LocalTime?.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        writer.WriteBoolean("localTimeIsApproximate", occurrence.Moment.IsApproximate);
        writer.WriteString("status", occurrence.Status.ToString());
        writer.WriteString("source", occurrence.Source.ToString());
        writer.WriteString("historicalConsistency", occurrence.HistoricalConsistency.ToString());
        WriteNullableString(writer, "historicalTargetName", occurrence.HistoricalTarget?.Name);
        WriteNullableString(writer, "historicalTargetCategory", occurrence.HistoricalTarget?.Category);
        WriteNullableString(writer, "privateNote", occurrence.PrivateNote);
        writer.WriteNumber("version", occurrence.Version);
        writer.WriteString("createdAtUtc", FormatUtc(occurrence.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(occurrence.UpdatedAtUtc));
        WriteNullableTimestamp(writer, "deletedAtUtc", occurrence.DeletedAtUtc);
        writer.WriteEndObject();
    }

    private static void WriteVisitAssessment(
        Utf8JsonWriter writer,
        Visit visit,
        IReadOnlyDictionary<string, Park> parks)
    {
        VisitParkAssessment assessment = visit.ParkAssessment!;
        ResolvePark(parks, visit.ParkId, out string parkName, out string parkStatus);
        writer.WriteStartObject();
        writer.WriteString("visitId", visit.Id.Value);
        writer.WriteString("parkId", visit.ParkId);
        writer.WriteString("parkName", parkName);
        writer.WriteString("parkStatus", parkStatus);
        writer.WriteNumber("valueHalfSteps", assessment.Value.HalfSteps);
        writer.WriteNumber("value", assessment.Value.DecimalValue);
        WriteNullableString(writer, "privateComment", assessment.PrivateComment);
        writer.WriteNumber("revision", assessment.Revision);
        writer.WriteString("createdAtUtc", FormatUtc(assessment.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(assessment.UpdatedAtUtc));
        writer.WriteEndObject();
    }

    private static void WriteRideAssessment(
        Utf8JsonWriter writer,
        RideOccurrence occurrence,
        IReadOnlyDictionary<string, VisitTarget> parkItems)
    {
        RideAssessment assessment = occurrence.Assessment!;
        ResolveParkItem(parkItems, occurrence, out string itemName, out string itemCategory, out string itemStatus);
        writer.WriteStartObject();
        writer.WriteString("rideOccurrenceId", occurrence.Id.Value);
        writer.WriteString("visitId", occurrence.VisitId.Value);
        writer.WriteString("parkItemId", occurrence.ParkItemId);
        writer.WriteString("parkItemName", itemName);
        writer.WriteString("parkItemCategory", itemCategory);
        writer.WriteString("parkItemStatus", itemStatus);
        writer.WriteNumber("valueHalfSteps", assessment.Value.HalfSteps);
        writer.WriteNumber("value", assessment.Value.DecimalValue);
        WriteNullableString(writer, "privateComment", assessment.PrivateComment);
        writer.WriteNumber("revision", assessment.Revision);
        writer.WriteString("createdAtUtc", FormatUtc(assessment.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(assessment.UpdatedAtUtc));
        writer.WriteEndObject();
    }

    private static void WriteVisitsCsv(ZipArchive archive, PassportExportWriteRequest request)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "visits.csv");
        WriteCsvRow(writer, new[]
        {
            "id", "parkId", "parkName", "parkStatus", "year", "month", "day",
            "datePrecision", "dateIsApproximate", "timeZoneId", "serviceDayConvention",
            "status", "privacy", "title", "privateNote", "version", "createdAtUtc",
            "updatedAtUtc", "completedAtUtc",
        });
        foreach (Visit visit in request.Visits)
        {
            ResolvePark(request.Parks, visit.ParkId, out string parkName, out string parkStatus);
            WriteCsvRow(writer, new[]
            {
                visit.Id.Value, visit.ParkId, parkName, parkStatus,
                Integer(visit.Date.Year), NullableInteger(visit.Date.Month), NullableInteger(visit.Date.Day),
                visit.Date.Precision.ToString(), Boolean(visit.Date.IsApproximate), visit.TimeZoneId,
                visit.ServiceDayConvention.ToString(), visit.Status.ToString(), visit.Privacy.ToString(),
                visit.Title, visit.PrivateNote, Integer(visit.Version), FormatUtc(visit.CreatedAtUtc),
                FormatUtc(visit.UpdatedAtUtc), NullableTimestamp(visit.CompletedAtUtc),
            });
        }
    }

    private static void WriteOccurrencesCsv(ZipArchive archive, PassportExportWriteRequest request)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "ride-occurrences.csv");
        WriteCsvRow(writer, new[]
        {
            "id", "visitId", "parkId", "parkName", "parkStatus", "parkItemId",
            "parkItemName", "parkItemCategory", "parkItemStatus", "sortPosition", "localTime",
            "localTimeIsApproximate", "status", "source", "historicalConsistency",
            "historicalTargetName", "historicalTargetCategory", "privateNote", "version",
            "createdAtUtc", "updatedAtUtc", "deletedAtUtc",
        });
        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            ResolvePark(request.Parks, occurrence.ParkId, out string parkName, out string parkStatus);
            ResolveParkItem(request.ParkItems, occurrence, out string itemName, out string itemCategory, out string itemStatus);
            WriteCsvRow(writer, new[]
            {
                occurrence.Id.Value, occurrence.VisitId.Value, occurrence.ParkId, parkName, parkStatus,
                occurrence.ParkItemId, itemName, itemCategory, itemStatus, Integer(occurrence.SortPosition),
                occurrence.Moment.LocalTime?.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
                Boolean(occurrence.Moment.IsApproximate), occurrence.Status.ToString(), occurrence.Source.ToString(),
                occurrence.HistoricalConsistency.ToString(), occurrence.HistoricalTarget?.Name,
                occurrence.HistoricalTarget?.Category, occurrence.PrivateNote, Integer(occurrence.Version),
                FormatUtc(occurrence.CreatedAtUtc), FormatUtc(occurrence.UpdatedAtUtc),
                NullableTimestamp(occurrence.DeletedAtUtc),
            });
        }
    }

    private static void WriteVisitAssessmentsCsv(ZipArchive archive, PassportExportWriteRequest request)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "visit-assessments.csv");
        WriteCsvRow(writer, new[]
        {
            "visitId", "parkId", "parkName", "parkStatus", "valueHalfSteps", "value",
            "privateComment", "revision", "createdAtUtc", "updatedAtUtc",
        });
        foreach (Visit visit in request.Visits.Where(static visit => visit.ParkAssessment is not null))
        {
            VisitParkAssessment assessment = visit.ParkAssessment!;
            ResolvePark(request.Parks, visit.ParkId, out string parkName, out string parkStatus);
            WriteCsvRow(writer, new[]
            {
                visit.Id.Value, visit.ParkId, parkName, parkStatus, Integer(assessment.Value.HalfSteps),
                assessment.Value.ToString(), assessment.PrivateComment, Integer(assessment.Revision),
                FormatUtc(assessment.CreatedAtUtc), FormatUtc(assessment.UpdatedAtUtc),
            });
        }
    }

    private static void WriteRideAssessmentsCsv(ZipArchive archive, PassportExportWriteRequest request)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "ride-assessments.csv");
        WriteCsvRow(writer, new[]
        {
            "rideOccurrenceId", "visitId", "parkItemId", "parkItemName", "parkItemCategory",
            "parkItemStatus", "valueHalfSteps", "value", "privateComment", "revision",
            "createdAtUtc", "updatedAtUtc",
        });
        foreach (RideOccurrence occurrence in request.RideOccurrences.Where(
                     static occurrence => occurrence.Assessment is not null))
        {
            RideAssessment assessment = occurrence.Assessment!;
            ResolveParkItem(request.ParkItems, occurrence, out string itemName, out string itemCategory, out string itemStatus);
            WriteCsvRow(writer, new[]
            {
                occurrence.Id.Value, occurrence.VisitId.Value, occurrence.ParkItemId, itemName, itemCategory,
                itemStatus, Integer(assessment.Value.HalfSteps), assessment.Value.ToString(),
                assessment.PrivateComment, Integer(assessment.Revision), FormatUtc(assessment.CreatedAtUtc),
                FormatUtc(assessment.UpdatedAtUtc),
            });
        }
    }

    private static StreamWriter CreateCsvWriter(ZipArchive archive, string fileName)
    {
        ZipArchiveEntry entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
        return new StreamWriter(entry.Open(), Utf8WithoutBom, bufferSize: 16 * 1024, leaveOpen: false)
        {
            NewLine = "\r\n",
        };
    }

    private static void WriteCsvRow(StreamWriter writer, IReadOnlyCollection<string?> values)
    {
        writer.WriteLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string? value)
    {
        string normalized = value ?? string.Empty;
        return normalized.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
            ? normalized
            : $"\"{normalized.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static void ResolvePark(
        IReadOnlyDictionary<string, Park> parks,
        string parkId,
        out string name,
        out string status)
    {
        if (parks.TryGetValue(parkId, out Park? park))
        {
            name = string.IsNullOrWhiteSpace(park.Name) ? parkId : park.Name.Trim();
            status = park.Status.ToString();
            return;
        }

        name = parkId;
        status = "Unavailable";
    }

    private static void ResolveParkItem(
        IReadOnlyDictionary<string, VisitTarget> parkItems,
        RideOccurrence occurrence,
        out string name,
        out string category,
        out string status)
    {
        if (parkItems.TryGetValue(occurrence.ParkItemId, out VisitTarget? target))
        {
            name = target.Name;
            category = target.Category.ToString();
            status = target.LifecycleStatus ?? "Current";
            return;
        }

        name = occurrence.HistoricalTarget?.Name ?? occurrence.ParkItemId;
        category = occurrence.HistoricalTarget?.Category ?? "Unknown";
        status = occurrence.HistoricalTarget is null ? "Unavailable" : "Historical";
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string propertyName, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
        else
        {
            writer.WriteNull(propertyName);
        }
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string propertyName, string? value)
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

    private static void WriteNullableTimestamp(Utf8JsonWriter writer, string propertyName, DateTime? value)
    {
        WriteNullableString(writer, propertyName, NullableTimestamp(value));
    }

    private static string FormatUtc(DateTime value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static string? NullableTimestamp(DateTime? value)
    {
        return value.HasValue ? FormatUtc(value.Value) : null;
    }

    private static string NullableInteger(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string Integer(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Boolean(bool value)
    {
        return value ? "true" : "false";
    }
}
