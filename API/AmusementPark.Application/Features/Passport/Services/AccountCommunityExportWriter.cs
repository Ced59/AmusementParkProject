using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class AccountCommunityExportWriter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

    public static IReadOnlyCollection<string> CsvFileNames { get; } = new[]
    {
        "comments.csv",
        "comment-bodies.csv",
        "contributed-images.csv",
        "contributed-image-localizations.csv",
        "historical-existence-reports.csv",
        "social-share-events.csv",
    };

    public static void WriteJson(Utf8JsonWriter writer, AccountCommunityExportData data)
    {
        writer.WriteStartArray("comments");
        foreach (AccountCommentExportData comment in data.Comments)
        {
            writer.WriteStartObject();
            writer.WriteString("reference", comment.Reference);
            writer.WriteString("targetType", comment.TargetType);
            WriteNullableString(writer, "targetName", comment.TargetName);
            WriteNullableString(writer, "parkName", comment.ParkName);
            WriteLocalizedTexts(writer, "bodies", comment.Bodies);
            writer.WriteStartArray("imageReferences");
            foreach (string reference in comment.ImageReferences)
            {
                writer.WriteStringValue(reference);
            }

            writer.WriteEndArray();
            writer.WriteBoolean("isOfficial", comment.IsOfficial);
            writer.WriteString("moderationStatus", comment.ModerationStatus);
            writer.WriteNumber("revision", comment.Revision);
            writer.WriteString("createdAtUtc", FormatUtc(comment.CreatedAtUtc));
            writer.WriteString("updatedAtUtc", FormatUtc(comment.UpdatedAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("contributedImages");
        foreach (AccountImageExportData image in data.Images)
        {
            writer.WriteStartObject();
            writer.WriteString("reference", image.Reference);
            WriteNullableString(writer, "originalFileName", image.OriginalFileName);
            WriteNullableString(writer, "contentType", image.ContentType);
            WriteNullableString(writer, "description", image.Description);
            WriteLocalizedTexts(writer, "alternativeTexts", image.AlternativeTexts);
            WriteLocalizedTexts(writer, "captions", image.Captions);
            WriteLocalizedTexts(writer, "credits", image.Credits);
            writer.WriteNumber("width", image.Width);
            writer.WriteNumber("height", image.Height);
            writer.WriteNumber("sizeInBytes", image.SizeInBytes);
            writer.WriteBoolean("isPublished", image.IsPublished);
            WriteNullableString(writer, "sourceUrl", image.SourceUrl);
            WriteNullableNumber(writer, "latitude", image.Latitude);
            WriteNullableNumber(writer, "longitude", image.Longitude);
            WriteNullableString(writer, "cameraMaker", image.CameraMaker);
            WriteNullableString(writer, "cameraModel", image.CameraModel);
            WriteNullableTimestamp(writer, "takenOnUtc", image.TakenOnUtc);
            writer.WriteString("createdAtUtc", FormatUtc(image.CreatedAtUtc));
            writer.WriteString("updatedAtUtc", FormatUtc(image.UpdatedAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("historicalExistenceReports");
        foreach (AccountHistoricalReportExportData report in data.HistoricalReports)
        {
            writer.WriteStartObject();
            writer.WriteString("parkName", report.ParkName);
            writer.WriteNumber("visitYear", report.VisitYear);
            WriteNullableNumber(writer, "visitMonth", report.VisitMonth);
            WriteNullableNumber(writer, "visitDay", report.VisitDay);
            writer.WriteString("visitDatePrecision", report.VisitDatePrecision);
            writer.WriteBoolean("visitDateIsApproximate", report.VisitDateIsApproximate);
            writer.WriteString("claimedName", report.ClaimedName);
            WriteNullableString(writer, "sourceUrl", report.SourceUrl);
            WriteNullableString(writer, "sourceReference", report.SourceReference);
            WriteNullableString(writer, "details", report.Details);
            writer.WriteString("status", report.Status);
            writer.WriteString("submittedAtUtc", FormatUtc(report.SubmittedAtUtc));
            WriteNullableTimestamp(writer, "reviewedAtUtc", report.ReviewedAtUtc);
            WriteNullableString(writer, "decisionNote", report.DecisionNote);
            writer.WriteNumber("revision", report.Revision);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("socialShareEvents");
        foreach (AccountSocialShareExportData socialShare in data.SocialShares)
        {
            writer.WriteStartObject();
            writer.WriteString("occurredAtUtc", FormatUtc(socialShare.OccurredAtUtc));
            writer.WriteString("targetType", socialShare.TargetType);
            WriteNullableString(writer, "targetTitle", socialShare.TargetTitle);
            WriteNullableString(writer, "languageCode", socialShare.LanguageCode);
            writer.WriteString("channel", socialShare.Channel);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static void WriteCsvEntries(ZipArchive archive, AccountCommunityExportData data)
    {
        WriteCommentsCsv(archive, data.Comments);
        WriteCommentBodiesCsv(archive, data.Comments);
        WriteImagesCsv(archive, data.Images);
        WriteImageLocalizationsCsv(archive, data.Images);
        WriteHistoricalReportsCsv(archive, data.HistoricalReports);
        WriteSocialSharesCsv(archive, data.SocialShares);
    }

    private static void WriteCommentsCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountCommentExportData> comments)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "comments.csv");
        WriteCsvRow(writer, new[]
        {
            "reference", "targetType", "targetName", "parkName", "imageReferences", "isOfficial",
            "moderationStatus", "revision", "createdAtUtc", "updatedAtUtc",
        });
        foreach (AccountCommentExportData comment in comments)
        {
            WriteCsvRow(writer, new[]
            {
                comment.Reference, comment.TargetType, comment.TargetName, comment.ParkName,
                string.Join("|", comment.ImageReferences), Boolean(comment.IsOfficial),
                comment.ModerationStatus, Integer(comment.Revision), FormatUtc(comment.CreatedAtUtc),
                FormatUtc(comment.UpdatedAtUtc),
            });
        }
    }

    private static void WriteCommentBodiesCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountCommentExportData> comments)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "comment-bodies.csv");
        WriteCsvRow(writer, new[] { "commentReference", "language", "body" });
        foreach (AccountCommentExportData comment in comments)
        {
            foreach (AccountLocalizedTextExportData body in comment.Bodies)
            {
                WriteCsvRow(writer, new[] { comment.Reference, body.Language, body.Value });
            }
        }
    }

    private static void WriteImagesCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountImageExportData> images)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "contributed-images.csv");
        WriteCsvRow(writer, new[]
        {
            "reference", "originalFileName", "contentType", "description", "width", "height",
            "sizeInBytes", "isPublished", "sourceUrl", "latitude", "longitude", "cameraMaker",
            "cameraModel", "takenOnUtc", "createdAtUtc", "updatedAtUtc",
        });
        foreach (AccountImageExportData image in images)
        {
            WriteCsvRow(writer, new[]
            {
                image.Reference, image.OriginalFileName, image.ContentType, image.Description,
                Integer(image.Width), Integer(image.Height), Integer(image.SizeInBytes),
                Boolean(image.IsPublished), image.SourceUrl, Number(image.Latitude), Number(image.Longitude),
                image.CameraMaker, image.CameraModel, Timestamp(image.TakenOnUtc),
                FormatUtc(image.CreatedAtUtc), FormatUtc(image.UpdatedAtUtc),
            });
        }
    }

    private static void WriteImageLocalizationsCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountImageExportData> images)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "contributed-image-localizations.csv");
        WriteCsvRow(writer, new[] { "imageReference", "kind", "language", "value" });
        foreach (AccountImageExportData image in images)
        {
            WriteLocalizedRows(writer, image.Reference, "AlternativeText", image.AlternativeTexts);
            WriteLocalizedRows(writer, image.Reference, "Caption", image.Captions);
            WriteLocalizedRows(writer, image.Reference, "Credit", image.Credits);
        }
    }

    private static void WriteHistoricalReportsCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountHistoricalReportExportData> reports)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "historical-existence-reports.csv");
        WriteCsvRow(writer, new[]
        {
            "parkName", "visitYear", "visitMonth", "visitDay", "visitDatePrecision",
            "visitDateIsApproximate", "claimedName", "sourceUrl", "sourceReference", "details", "status",
            "submittedAtUtc", "reviewedAtUtc", "decisionNote", "revision",
        });
        foreach (AccountHistoricalReportExportData report in reports)
        {
            WriteCsvRow(writer, new[]
            {
                report.ParkName, Integer(report.VisitYear), Integer(report.VisitMonth), Integer(report.VisitDay),
                report.VisitDatePrecision, Boolean(report.VisitDateIsApproximate), report.ClaimedName,
                report.SourceUrl, report.SourceReference, report.Details, report.Status,
                FormatUtc(report.SubmittedAtUtc), Timestamp(report.ReviewedAtUtc), report.DecisionNote,
                Integer(report.Revision),
            });
        }
    }

    private static void WriteSocialSharesCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountSocialShareExportData> socialShares)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "social-share-events.csv");
        WriteCsvRow(writer, new[]
        {
            "occurredAtUtc", "targetType", "targetTitle", "languageCode", "channel",
        });
        foreach (AccountSocialShareExportData socialShare in socialShares)
        {
            WriteCsvRow(writer, new[]
            {
                FormatUtc(socialShare.OccurredAtUtc), socialShare.TargetType, socialShare.TargetTitle,
                socialShare.LanguageCode, socialShare.Channel,
            });
        }
    }

    private static void WriteLocalizedTexts(
        Utf8JsonWriter writer,
        string propertyName,
        IReadOnlyCollection<AccountLocalizedTextExportData> values)
    {
        writer.WriteStartArray(propertyName);
        foreach (AccountLocalizedTextExportData value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("language", value.Language);
            WriteNullableString(writer, "value", value.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteLocalizedRows(
        StreamWriter writer,
        string imageReference,
        string kind,
        IReadOnlyCollection<AccountLocalizedTextExportData> values)
    {
        foreach (AccountLocalizedTextExportData value in values)
        {
            WriteCsvRow(writer, new[] { imageReference, kind, value.Language, value.Value });
        }
    }

    private static StreamWriter CreateCsvWriter(ZipArchive archive, string fileName)
    {
        ZipArchiveEntry entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
        return new StreamWriter(entry.Open(), Utf8WithoutBom, 16 * 1024, false)
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
        if (normalized.Length > 0 && normalized[0] is '=' or '+' or '-' or '@')
        {
            normalized = $"'{normalized}";
        }

        return normalized.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
            ? normalized
            : $"\"{normalized.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
            return;
        }

        writer.WriteString(name, value);
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string name, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(name, value.Value);
            return;
        }

        writer.WriteNull(name);
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string name, double? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(name, value.Value);
            return;
        }

        writer.WriteNull(name);
    }

    private static void WriteNullableTimestamp(Utf8JsonWriter writer, string name, DateTime? value)
    {
        WriteNullableString(writer, name, Timestamp(value));
    }

    private static string FormatUtc(DateTime value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static string? Timestamp(DateTime? value)
    {
        return value.HasValue ? FormatUtc(value.Value) : null;
    }

    private static string Boolean(bool value)
    {
        return value ? "true" : "false";
    }

    private static string Integer(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Integer(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string? Integer(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    private static string? Number(double? value)
    {
        return value?.ToString("R", CultureInfo.InvariantCulture);
    }
}
