using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportShareLifecycleExportWriter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);
    private static readonly char[] CsvSpecialCharacters = { ',', '"', '\r', '\n' };

    public static IReadOnlyCollection<string> CsvFileNames { get; } = new[]
    {
        "share-publications.csv",
        "comparison-invitations.csv",
        "comparisons.csv",
        "comparison-parks.csv",
        "comparison-ratings.csv",
        "comparison-years.csv",
        "comparison-missed-items.csv",
    }.Concat(PassportShareSnapshotExportWriter.CsvFileNames).ToArray();

    public static void WriteJson(
        Utf8JsonWriter writer,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        writer.WriteStartArray("sharePublications");
        foreach (SharePublication publication in request.ShareLifecycle.Publications)
        {
            WritePublication(writer, request.UserId, publication, references);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("comparisonInvitations");
        foreach (ProfileComparisonInvitation invitation in request.ShareLifecycle.Invitations)
        {
            WriteInvitation(writer, request.UserId, invitation, references);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("comparisons");
        foreach (ProfileComparison comparison in request.ShareLifecycle.Comparisons)
        {
            WriteComparison(writer, request.UserId, comparison, references);
        }

        writer.WriteEndArray();
        PassportShareSnapshotExportWriter.WriteJson(
            writer,
            request.ShareLifecycle,
            references);
    }

    public static void WriteCsvEntries(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        WritePublicationsCsv(archive, request, references);
        WriteInvitationsCsv(archive, request, references);
        WriteComparisonsCsv(archive, request, references);
        PassportShareLifecycleComparisonCsvWriter.WriteCsvEntries(
            archive,
            request,
            references);
        PassportShareSnapshotExportWriter.WriteCsvEntries(
            archive,
            request.ShareLifecycle,
            references);
    }

    private static void WritePublication(
        Utf8JsonWriter writer,
        string userId,
        SharePublication publication,
        PassportExportReferenceMap references)
    {
        ResolveSource(userId, publication, references, out string sourceKind,
            out string? visitReference, out int? year);
        writer.WriteStartObject();
        writer.WriteString("reference", references.Publication(publication.Id));
        writer.WriteString("type", publication.Type.ToString());
        writer.WriteString("status", publication.Status.ToString());
        writer.WriteString("visibility", publication.Visibility.ToString());
        writer.WriteString("sourceKind", sourceKind);
        WriteNullableString(writer, "sourceVisitReference", visitReference);
        WriteNullableNumber(writer, "sourceYear", year);
        writer.WriteStartObject("policy");
        writer.WriteNumber("schemaVersion", publication.ContentPolicy.SchemaVersion);
        writer.WriteString("datePrecision", publication.ContentPolicy.DatePrecision.ToString());
        writer.WriteStartArray("includedFields");
        foreach (ShareContentField field in publication.ContentPolicy.IncludedFields)
        {
            writer.WriteStringValue(field.ToString());
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteNumber("sourceVersion", publication.SourceVersion);
        writer.WriteNumber("publicationVersion", publication.PublicationVersion);
        writer.WriteNumber("version", publication.Version);
        writer.WriteBoolean("isPubliclyResolvable", publication.IsResolvable);
        writer.WriteBoolean("isModerationSuspended", publication.IsModerationSuspended);
        WriteNullableTimestamp(writer, "publishedAtUtc", publication.PublishedAtUtc);
        WriteNullableTimestamp(writer, "revokedAtUtc", publication.RevokedAtUtc);
        writer.WriteString("createdAtUtc", FormatUtc(publication.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(publication.UpdatedAtUtc));
        writer.WriteEndObject();
    }

    private static void WriteInvitation(
        Utf8JsonWriter writer,
        string userId,
        ProfileComparisonInvitation invitation,
        PassportExportReferenceMap references)
    {
        bool isCreator = IsCreator(userId, invitation.CreatorUserId);
        SharePublicationId? ownPublicationId = isCreator
            ? invitation.CreatorPassportPublicationId
            : invitation.AcceptorPassportPublicationId;
        long? ownPublicationVersion = isCreator
            ? invitation.CreatorPassportPublicationVersion
            : invitation.AcceptorPassportPublicationVersion;
        writer.WriteStartObject();
        writer.WriteString("reference", references.Invitation(invitation.Id));
        writer.WriteString("role", isCreator ? "Creator" : "Acceptor");
        writer.WriteString("status", invitation.Status.ToString());
        WriteCategories(writer, invitation.Categories);
        WriteNullableString(
            writer,
            "yourPassportPublicationReference",
            references.PublicationOrDefault(ownPublicationId));
        WriteNullableNumber(writer, "yourPassportPublicationVersion", ownPublicationVersion);
        WriteNullableString(
            writer,
            "comparisonReference",
            references.ComparisonOrDefault(invitation.ComparisonId));
        writer.WriteString("expiresAtUtc", FormatUtc(invitation.ExpiresAtUtc));
        WriteNullableTimestamp(writer, "acceptedAtUtc", invitation.AcceptedAtUtc);
        writer.WriteString("createdAtUtc", FormatUtc(invitation.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(invitation.UpdatedAtUtc));
        writer.WriteNumber("version", invitation.Version);
        writer.WriteEndObject();
    }

    private static void WriteComparison(
        Utf8JsonWriter writer,
        string userId,
        ProfileComparison comparison,
        PassportExportReferenceMap references)
    {
        bool isCreator = IsCreator(userId, comparison.CreatorUserId);
        SharePublicationId ownPublicationId = isCreator
            ? comparison.CreatorPassportPublicationId
            : comparison.AcceptorPassportPublicationId;
        long ownPublicationVersion = isCreator
            ? comparison.CreatorPassportPublicationVersion
            : comparison.AcceptorPassportPublicationVersion;
        long otherPublicationVersion = isCreator
            ? comparison.AcceptorPassportPublicationVersion
            : comparison.CreatorPassportPublicationVersion;
        writer.WriteStartObject();
        writer.WriteString("reference", references.Comparison(comparison.Id));
        WriteNullableString(
            writer,
            "invitationReference",
            references.InvitationOrDefault(comparison.InvitationId));
        writer.WriteString("role", isCreator ? "Creator" : "Acceptor");
        writer.WriteString("status", comparison.Status.ToString());
        WriteCategories(writer, comparison.Calculation.Categories);
        WriteNullableString(
            writer,
            "yourPassportPublicationReference",
            references.PublicationOrDefault(ownPublicationId));
        writer.WriteNumber("yourPassportPublicationVersion", ownPublicationVersion);
        writer.WriteNumber("otherPassportPublicationVersion", otherPublicationVersion);
        writer.WriteBoolean("isPubliclyResolvable", comparison.IsPubliclyResolvable);
        writer.WriteBoolean("isModerationSuspended", comparison.IsModerationSuspended);
        writer.WriteBoolean(
            "wasRevokedByYou",
            string.Equals(comparison.RevokedByUserId, userId, StringComparison.Ordinal));
        writer.WriteNumber("version", comparison.Version);
        writer.WriteString("createdAtUtc", FormatUtc(comparison.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(comparison.UpdatedAtUtc));
        WriteNullableTimestamp(writer, "revokedAtUtc", comparison.RevokedAtUtc);
        WriteComparisonResults(writer, comparison, isCreator);
        writer.WriteEndObject();
    }

    private static void WriteComparisonResults(
        Utf8JsonWriter writer,
        ProfileComparison comparison,
        bool isCreator)
    {
        ProfileComparisonCalculation calculation = comparison.Calculation;
        writer.WriteStartObject("results");
        WriteNullableString(
            writer,
            "yourDisplayName",
            isCreator ? calculation.CreatorDisplayName : calculation.AcceptorDisplayName);
        WriteNullableString(
            writer,
            "otherMemberDisplayName",
            isCreator ? calculation.AcceptorDisplayName : calculation.CreatorDisplayName);
        writer.WriteNumber("commonRatingCount", calculation.CommonRatingCount);
        writer.WriteNumber(
            "minimumRatingsForCorrelation",
            calculation.MinimumRatingsForCorrelation);
        WriteNullableDouble(writer, "ratingCorrelation", calculation.RatingCorrelation);
        writer.WriteBoolean("hasIncompleteCatalog", calculation.HasIncompleteCatalog);
        writer.WriteString("calculationVersion", calculation.CalculationVersion);
        writer.WriteStartArray("parks");
        foreach (ProfileComparisonParkResult park in calculation.Parks)
        {
            writer.WriteStartObject();
            writer.WriteString("name", park.Name);
            WriteNullableString(writer, "countryCode", park.CountryCode);
            WriteNullableNumber(
                writer,
                "yourVisitCount",
                isCreator ? park.CreatorVisitCount : park.AcceptorVisitCount);
            WriteNullableNumber(
                writer,
                "otherMemberVisitCount",
                isCreator ? park.AcceptorVisitCount : park.CreatorVisitCount);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("ratings");
        foreach (ProfileComparisonRatingResult rating in calculation.Ratings)
        {
            writer.WriteStartObject();
            writer.WriteString("targetType", rating.TargetType);
            writer.WriteString("name", rating.Name);
            WriteNullableString(writer, "parkName", rating.ParkName);
            WriteNullableString(writer, "category", rating.Category);
            writer.WriteNumber(
                "yourRating",
                isCreator ? rating.CreatorRating : rating.AcceptorRating);
            writer.WriteNumber(
                "otherMemberRating",
                isCreator ? rating.AcceptorRating : rating.CreatorRating);
            writer.WriteNumber("absoluteDifference", rating.AbsoluteDifference);
            writer.WriteString("affinity", rating.Affinity.ToString());
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("years");
        foreach (ProfileComparisonYearResult year in calculation.Years)
        {
            writer.WriteStartObject();
            writer.WriteNumber("year", year.Year);
            writer.WriteNumber(
                "yourVisitCount",
                isCreator ? year.CreatorVisitCount : year.AcceptorVisitCount);
            writer.WriteNumber(
                "otherMemberVisitCount",
                isCreator ? year.AcceptorVisitCount : year.CreatorVisitCount);
            WriteNullableNumber(
                writer,
                "yourRideCount",
                isCreator ? year.CreatorRideCount : year.AcceptorRideCount);
            WriteNullableNumber(
                writer,
                "otherMemberRideCount",
                isCreator ? year.AcceptorRideCount : year.CreatorRideCount);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("missedItems");
        foreach (ProfileComparisonMissedItemResult item in calculation.MissedItems)
        {
            writer.WriteStartObject();
            writer.WriteString("name", item.Name);
            writer.WriteString("status", item.Status);
            WriteNullableNumber(
                writer,
                "yourOccurrenceCount",
                isCreator ? item.CreatorOccurrenceCount : item.AcceptorOccurrenceCount);
            WriteNullableNumber(
                writer,
                "otherMemberOccurrenceCount",
                isCreator ? item.AcceptorOccurrenceCount : item.CreatorOccurrenceCount);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WritePublicationsCsv(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "share-publications.csv");
        WriteCsvRow(writer, new[]
        {
            "reference", "type", "status", "visibility", "sourceKind",
            "sourceVisitReference", "sourceYear", "policySchemaVersion", "datePrecision",
            "includedFields", "sourceVersion", "publicationVersion", "version",
            "isPubliclyResolvable", "isModerationSuspended", "publishedAtUtc",
            "revokedAtUtc", "createdAtUtc", "updatedAtUtc",
        });
        foreach (SharePublication publication in request.ShareLifecycle.Publications)
        {
            ResolveSource(request.UserId, publication, references, out string sourceKind,
                out string? visitReference, out int? year);
            WriteCsvRow(writer, new[]
            {
                references.Publication(publication.Id), publication.Type.ToString(),
                publication.Status.ToString(), publication.Visibility.ToString(), sourceKind,
                visitReference, NullableInteger(year), Integer(publication.ContentPolicy.SchemaVersion),
                publication.ContentPolicy.DatePrecision.ToString(),
                Join(publication.ContentPolicy.IncludedFields), Integer(publication.SourceVersion),
                Integer(publication.PublicationVersion), Integer(publication.Version),
                Boolean(publication.IsResolvable), Boolean(publication.IsModerationSuspended),
                NullableTimestamp(publication.PublishedAtUtc),
                NullableTimestamp(publication.RevokedAtUtc), FormatUtc(publication.CreatedAtUtc),
                FormatUtc(publication.UpdatedAtUtc),
            });
        }
    }

    private static void WriteInvitationsCsv(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "comparison-invitations.csv");
        WriteCsvRow(writer, new[]
        {
            "reference", "role", "status", "categories", "yourPassportPublicationReference",
            "yourPassportPublicationVersion", "comparisonReference", "expiresAtUtc",
            "acceptedAtUtc", "createdAtUtc", "updatedAtUtc", "version",
        });
        foreach (ProfileComparisonInvitation invitation in request.ShareLifecycle.Invitations)
        {
            bool isCreator = IsCreator(request.UserId, invitation.CreatorUserId);
            SharePublicationId? publicationId = isCreator
                ? invitation.CreatorPassportPublicationId
                : invitation.AcceptorPassportPublicationId;
            long? publicationVersion = isCreator
                ? invitation.CreatorPassportPublicationVersion
                : invitation.AcceptorPassportPublicationVersion;
            WriteCsvRow(writer, new[]
            {
                references.Invitation(invitation.Id), isCreator ? "Creator" : "Acceptor",
                invitation.Status.ToString(), Join(invitation.Categories),
                references.PublicationOrDefault(publicationId), NullableInteger(publicationVersion),
                references.ComparisonOrDefault(invitation.ComparisonId),
                FormatUtc(invitation.ExpiresAtUtc), NullableTimestamp(invitation.AcceptedAtUtc),
                FormatUtc(invitation.CreatedAtUtc), FormatUtc(invitation.UpdatedAtUtc),
                Integer(invitation.Version),
            });
        }
    }

    private static void WriteComparisonsCsv(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "comparisons.csv");
        WriteCsvRow(writer, new[]
        {
            "reference", "invitationReference", "role", "status", "categories",
            "yourPassportPublicationReference", "yourPassportPublicationVersion",
            "otherPassportPublicationVersion", "yourDisplayName", "otherMemberDisplayName",
            "commonRatingCount", "minimumRatingsForCorrelation", "ratingCorrelation",
            "hasIncompleteCatalog", "calculationVersion", "isPubliclyResolvable",
            "isModerationSuspended", "wasRevokedByYou", "version", "createdAtUtc",
            "updatedAtUtc", "revokedAtUtc",
        });
        foreach (ProfileComparison comparison in request.ShareLifecycle.Comparisons)
        {
            bool isCreator = IsCreator(request.UserId, comparison.CreatorUserId);
            SharePublicationId publicationId = isCreator
                ? comparison.CreatorPassportPublicationId
                : comparison.AcceptorPassportPublicationId;
            long publicationVersion = isCreator
                ? comparison.CreatorPassportPublicationVersion
                : comparison.AcceptorPassportPublicationVersion;
            long otherPublicationVersion = isCreator
                ? comparison.AcceptorPassportPublicationVersion
                : comparison.CreatorPassportPublicationVersion;
            ProfileComparisonCalculation calculation = comparison.Calculation;
            WriteCsvRow(writer, new[]
            {
                references.Comparison(comparison.Id),
                references.InvitationOrDefault(comparison.InvitationId),
                isCreator ? "Creator" : "Acceptor", comparison.Status.ToString(),
                Join(calculation.Categories), references.PublicationOrDefault(publicationId),
                Integer(publicationVersion), Integer(otherPublicationVersion),
                isCreator ? calculation.CreatorDisplayName : calculation.AcceptorDisplayName,
                isCreator ? calculation.AcceptorDisplayName : calculation.CreatorDisplayName,
                Integer(calculation.CommonRatingCount),
                Integer(calculation.MinimumRatingsForCorrelation),
                NullableDouble(calculation.RatingCorrelation), Boolean(calculation.HasIncompleteCatalog),
                calculation.CalculationVersion, Boolean(comparison.IsPubliclyResolvable),
                Boolean(comparison.IsModerationSuspended), Boolean(string.Equals(
                    comparison.RevokedByUserId, request.UserId, StringComparison.Ordinal)),
                Integer(comparison.Version), FormatUtc(comparison.CreatedAtUtc),
                FormatUtc(comparison.UpdatedAtUtc), NullableTimestamp(comparison.RevokedAtUtc),
            });
        }
    }

    private static void ResolveSource(
        string userId,
        SharePublication publication,
        PassportExportReferenceMap references,
        out string sourceKind,
        out string? visitReference,
        out int? year)
    {
        visitReference = null;
        year = null;
        if (VisitRecapShareSourceScope.TryParse(
                publication.SourceScopeKey,
                out string visitOwner,
                out string visitId)
            && string.Equals(visitOwner, userId, StringComparison.Ordinal))
        {
            sourceKind = "Visit";
            visitReference = references.VisitOrDefault(visitId);
            return;
        }

        if (YearRecapShareSourceScope.TryParse(
                publication.SourceScopeKey,
                out string yearOwner,
                out int sourceYear)
            && string.Equals(yearOwner, userId, StringComparison.Ordinal))
        {
            sourceKind = "Year";
            year = sourceYear;
            return;
        }

        if (PassportProfileShareSourceScope.TryParse(
                publication.SourceScopeKey,
                out string passportOwner)
            && string.Equals(passportOwner, userId, StringComparison.Ordinal))
        {
            sourceKind = "Passport";
            return;
        }

        if (PersonalRankingShareSourceScope.TryParse(
                publication.SourceScopeKey,
                out string rankingOwner)
            && string.Equals(rankingOwner, userId, StringComparison.Ordinal))
        {
            sourceKind = "Ranking";
            return;
        }

        sourceKind = "Unknown";
    }

    private static void WriteCategories(
        Utf8JsonWriter writer,
        IEnumerable<ProfileComparisonCategory> categories)
    {
        writer.WriteStartArray("categories");
        foreach (ProfileComparisonCategory category in categories)
        {
            writer.WriteStringValue(category.ToString());
        }

        writer.WriteEndArray();
    }

    internal static bool IsCreator(string userId, string creatorUserId)
    {
        return string.Equals(userId, creatorUserId, StringComparison.Ordinal);
    }

    internal static StreamWriter CreateCsvWriter(ZipArchive archive, string fileName)
    {
        ZipArchiveEntry entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
        return new StreamWriter(entry.Open(), Utf8WithoutBom, 16 * 1024, leaveOpen: false)
        {
            NewLine = "\r\n",
        };
    }

    internal static void WriteCsvRow(StreamWriter writer, IReadOnlyCollection<string?> values)
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

        return normalized.IndexOfAny(CsvSpecialCharacters) < 0
            ? normalized
            : $"\"{normalized.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string Join<T>(IEnumerable<T> values)
    {
        return string.Join("|", values.Select(static value => value?.ToString()));
    }

    private static string FormatUtc(DateTime value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static string? NullableTimestamp(DateTime? value)
    {
        return value.HasValue ? FormatUtc(value.Value) : null;
    }

    internal static string Integer(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string NullableInteger(long? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    internal static string Double(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string NullableDouble(double? value)
    {
        return value.HasValue ? Double(value.Value) : string.Empty;
    }

    private static string Boolean(bool value)
    {
        return value ? "true" : "false";
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

    private static void WriteNullableNumber(
        Utf8JsonWriter writer,
        string propertyName,
        long? value)
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

    private static void WriteNullableDouble(
        Utf8JsonWriter writer,
        string propertyName,
        double? value)
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

    private static void WriteNullableTimestamp(
        Utf8JsonWriter writer,
        string propertyName,
        DateTime? value)
    {
        WriteNullableString(writer, propertyName, NullableTimestamp(value));
    }
}
