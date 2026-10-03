using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class FederatedAccountExportWriter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

    public static IReadOnlyCollection<string> CsvFileNames { get; } = new[]
    {
        "account-profile.csv",
        "account-linked-logins.csv",
        "global-ratings.csv",
        "park-fit-profiles.csv",
        "trips.csv",
        "trip-candidate-parks.csv",
        "trip-days.csv",
        "trip-day-blocks.csv",
        "trip-decisions.csv",
    }.Concat(AccountCommunityExportWriter.CsvFileNames).ToArray();

    public static void WriteJson(Utf8JsonWriter writer, PassportExportWriteRequest request)
    {
        FederatedAccountExportData data = request.AccountData;
        WriteIdentity(writer, data.Identity);
        writer.WriteStartArray("globalRatings");
        foreach (AccountRatingExportData rating in data.Ratings)
        {
            writer.WriteStartObject();
            writer.WriteString("targetType", rating.TargetType.ToString());
            writer.WriteString("targetName", rating.TargetName);
            WriteNullableString(writer, "parkName", rating.ParkName);
            WriteNullableString(writer, "parkItemCategory", rating.ParkItemCategory?.ToString());
            WriteNullableString(writer, "parkItemType", rating.ParkItemType?.ToString());
            writer.WriteNumber("value", rating.Value);
            writer.WriteString("updatedAtUtc", FormatUtc(rating.UpdatedAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("parkFitProfiles");
        foreach (AccountParkFitProfileExportData profile in data.ParkFitProfiles)
        {
            writer.WriteStartObject();
            writer.WriteString("alias", profile.Alias);
            WriteNullableNumber(writer, "heightCentimeters", profile.HeightCentimeters);
            WriteNullableNumber(writer, "ageYears", profile.AgeYears);
            writer.WriteBoolean("canBeAccompanied", profile.CanBeAccompanied);
            WriteNullableNumber(writer, "companionAgeYears", profile.CompanionAgeYears);
            writer.WriteString("createdAtUtc", FormatUtc(profile.CreatedAtUtc));
            writer.WriteString("updatedAtUtc", FormatUtc(profile.UpdatedAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("trips");
        foreach (AccountTripExportData trip in data.Trips)
        {
            WriteTrip(writer, trip);
        }

        writer.WriteEndArray();
        AccountCommunityExportWriter.WriteJson(writer, data.Community);
    }

    public static void WriteCsvEntries(ZipArchive archive, PassportExportWriteRequest request)
    {
        WriteIdentityCsv(archive, request.AccountData.Identity);
        WriteLinkedLoginsCsv(archive, request.AccountData.Identity);
        WriteRatingsCsv(archive, request.AccountData.Ratings);
        WriteParkFitProfilesCsv(archive, request.AccountData.ParkFitProfiles);
        WriteTripsCsv(archive, request.AccountData.Trips);
        WriteTripCandidatesCsv(archive, request.AccountData.Trips);
        WriteTripDaysCsv(archive, request.AccountData.Trips);
        WriteTripDayBlocksCsv(archive, request.AccountData.Trips);
        WriteTripDecisionsCsv(archive, request.AccountData.Trips);
        AccountCommunityExportWriter.WriteCsvEntries(archive, request.AccountData.Community);
    }

    private static void WriteIdentity(Utf8JsonWriter writer, AccountIdentityExportData identity)
    {
        writer.WriteStartObject("account");
        WriteNullableString(writer, "firstName", identity.FirstName);
        WriteNullableString(writer, "lastName", identity.LastName);
        WriteNullableString(writer, "publicDisplayName", identity.PublicDisplayName);
        WriteNullableString(writer, "email", identity.Email);
        writer.WriteBoolean("isActivated", identity.IsActivated);
        writer.WriteBoolean("isBlocked", identity.IsBlocked);
        WriteNullableString(writer, "preferredLanguage", identity.PreferredLanguage);
        WriteNullableString(writer, "preferredMeasurementSystem", identity.PreferredMeasurementSystem);
        writer.WriteBoolean("hasAvatar", identity.HasAvatar);
        writer.WriteStartArray("roles");
        foreach (string role in identity.Roles)
        {
            writer.WriteStringValue(role);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("linkedLogins");
        foreach (AccountLinkedLoginExportData login in identity.LinkedLogins)
        {
            writer.WriteStartObject();
            writer.WriteString("provider", login.Provider);
            WriteNullableString(writer, "email", login.Email);
            writer.WriteBoolean("isEmailVerified", login.IsEmailVerified);
            WriteNullableString(writer, "displayName", login.DisplayName);
            writer.WriteString("linkedAtUtc", FormatUtc(login.LinkedAtUtc));
            writer.WriteString("lastLoginAtUtc", FormatUtc(login.LastLoginAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteString("createdAtUtc", FormatUtc(identity.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(identity.UpdatedAtUtc));
        writer.WriteString("lastLoginUtc", FormatUtc(identity.LastLoginUtc));
        writer.WriteString("lastActivityUtc", FormatUtc(identity.LastActivityUtc));
        writer.WriteEndObject();
    }

    private static void WriteTrip(Utf8JsonWriter writer, AccountTripExportData data)
    {
        TripExportResult trip = data.Trip;
        writer.WriteStartObject();
        writer.WriteString("reference", data.Reference);
        writer.WriteString("membershipRole", data.MembershipRole);
        writer.WriteString("title", trip.Title);
        writer.WriteString("status", trip.Status.ToString());
        WriteNullableString(writer, "destinationTimeZoneId", trip.DestinationTimeZoneId);
        writer.WriteString("generatedAtUtc", FormatUtc(trip.GeneratedAtUtc));
        writer.WriteStartObject("dateProposal");
        writer.WriteString("kind", trip.DateProposal.Kind.ToString());
        WriteNullableDate(writer, "startDate", trip.DateProposal.StartDate);
        WriteNullableDate(writer, "endDate", trip.DateProposal.EndDate);
        writer.WriteStartArray("candidateDates");
        foreach (DateOnly date in trip.DateProposal.CandidateDates)
        {
            writer.WriteStringValue(FormatDate(date));
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteStartArray("candidateParks");
        foreach (TripExportCandidateResult candidate in trip.CandidateParks)
        {
            writer.WriteStartObject();
            WriteNullableString(writer, "parkName", candidate.ParkName);
            writer.WriteBoolean("isParkAvailable", candidate.IsParkAvailable);
            writer.WriteString("state", candidate.State.ToString());
            WriteNullableString(writer, "collectiveNote", candidate.CollectiveNote);
            writer.WriteStartArray("candidateDates");
            foreach (DateOnly date in candidate.CandidateDates)
            {
                writer.WriteStringValue(FormatDate(date));
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("days");
        foreach (TripExportDayResult day in trip.Days)
        {
            writer.WriteStartObject();
            writer.WriteString("localDate", FormatDate(day.LocalDate));
            WriteNullableString(writer, "parkName", day.ParkName);
            writer.WriteBoolean("isParkAvailable", day.IsParkAvailable);
            WriteNullableString(writer, "desiredArrivalTime", FormatTime(day.DesiredArrivalTime));
            WriteNullableString(writer, "groupNote", day.GroupNote);
            writer.WriteStartArray("blocks");
            foreach (TripExportDayBlockResult block in day.Blocks)
            {
                writer.WriteStartObject();
                writer.WriteString("type", block.Type.ToString());
                writer.WriteString("title", block.Title);
                WriteNullableString(writer, "details", block.Details);
                WriteNullableString(writer, "localTime", FormatTime(block.LocalTime));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("collectiveDecisions");
        foreach (TripExportDecisionResult decision in trip.CollectiveDecisions)
        {
            writer.WriteStartObject();
            WriteNullableString(writer, "parkName", decision.ParkName);
            WriteNullableString(writer, "parkItemName", decision.ParkItemName);
            writer.WriteBoolean("isParkItemAvailable", decision.IsParkItemAvailable);
            writer.WriteString("status", decision.Status.ToString());
            writer.WriteString("reason", decision.Reason);
            writer.WriteString("decidedAtUtc", FormatUtc(decision.DecidedAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteIdentityCsv(ZipArchive archive, AccountIdentityExportData identity)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "account-profile.csv");
        WriteCsvRow(writer, new[]
        {
            "firstName", "lastName", "publicDisplayName", "email", "isActivated", "isBlocked",
            "preferredLanguage", "preferredMeasurementSystem", "hasAvatar", "roles", "createdAtUtc",
            "updatedAtUtc", "lastLoginUtc", "lastActivityUtc",
        });
        WriteCsvRow(writer, new[]
        {
            identity.FirstName, identity.LastName, identity.PublicDisplayName, identity.Email,
            Boolean(identity.IsActivated), Boolean(identity.IsBlocked), identity.PreferredLanguage,
            identity.PreferredMeasurementSystem, Boolean(identity.HasAvatar), string.Join("|", identity.Roles),
            FormatUtc(identity.CreatedAtUtc), FormatUtc(identity.UpdatedAtUtc),
            FormatUtc(identity.LastLoginUtc), FormatUtc(identity.LastActivityUtc),
        });
    }

    private static void WriteLinkedLoginsCsv(ZipArchive archive, AccountIdentityExportData identity)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "account-linked-logins.csv");
        WriteCsvRow(writer, new[]
        {
            "provider", "email", "isEmailVerified", "displayName", "linkedAtUtc", "lastLoginAtUtc",
        });
        foreach (AccountLinkedLoginExportData login in identity.LinkedLogins)
        {
            WriteCsvRow(writer, new[]
            {
                login.Provider, login.Email, Boolean(login.IsEmailVerified), login.DisplayName,
                FormatUtc(login.LinkedAtUtc), FormatUtc(login.LastLoginAtUtc),
            });
        }
    }

    private static void WriteRatingsCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountRatingExportData> ratings)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "global-ratings.csv");
        WriteCsvRow(writer, new[]
        {
            "targetType", "targetName", "parkName", "parkItemCategory", "parkItemType", "value",
            "updatedAtUtc",
        });
        foreach (AccountRatingExportData rating in ratings)
        {
            WriteCsvRow(writer, new[]
            {
                rating.TargetType.ToString(), rating.TargetName, rating.ParkName,
                rating.ParkItemCategory?.ToString(), rating.ParkItemType?.ToString(),
                rating.Value.ToString("0.0", CultureInfo.InvariantCulture), FormatUtc(rating.UpdatedAtUtc),
            });
        }
    }

    private static void WriteParkFitProfilesCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountParkFitProfileExportData> profiles)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "park-fit-profiles.csv");
        WriteCsvRow(writer, new[]
        {
            "alias", "heightCentimeters", "ageYears", "canBeAccompanied", "companionAgeYears",
            "createdAtUtc", "updatedAtUtc",
        });
        foreach (AccountParkFitProfileExportData profile in profiles)
        {
            WriteCsvRow(writer, new[]
            {
                profile.Alias, Integer(profile.HeightCentimeters), Integer(profile.AgeYears),
                Boolean(profile.CanBeAccompanied), Integer(profile.CompanionAgeYears),
                FormatUtc(profile.CreatedAtUtc), FormatUtc(profile.UpdatedAtUtc),
            });
        }
    }

    private static void WriteTripsCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountTripExportData> trips)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "trips.csv");
        WriteCsvRow(writer, new[]
        {
            "reference", "membershipRole", "title", "status", "destinationTimeZoneId",
            "dateProposalKind", "startDate", "endDate", "candidateDates", "generatedAtUtc",
        });
        foreach (AccountTripExportData data in trips)
        {
            TripExportResult trip = data.Trip;
            WriteCsvRow(writer, new[]
            {
                data.Reference, data.MembershipRole, trip.Title, trip.Status.ToString(),
                trip.DestinationTimeZoneId, trip.DateProposal.Kind.ToString(),
                Date(trip.DateProposal.StartDate), Date(trip.DateProposal.EndDate),
                string.Join("|", trip.DateProposal.CandidateDates.Select(FormatDate)),
                FormatUtc(trip.GeneratedAtUtc),
            });
        }
    }

    private static void WriteTripCandidatesCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountTripExportData> trips)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "trip-candidate-parks.csv");
        WriteCsvRow(writer, new[]
        {
            "tripReference", "parkName", "isParkAvailable", "candidateDates", "state", "collectiveNote",
        });
        foreach (AccountTripExportData data in trips)
        {
            foreach (TripExportCandidateResult candidate in data.Trip.CandidateParks)
            {
                WriteCsvRow(writer, new[]
                {
                    data.Reference, candidate.ParkName, Boolean(candidate.IsParkAvailable),
                    string.Join("|", candidate.CandidateDates.Select(FormatDate)), candidate.State.ToString(),
                    candidate.CollectiveNote,
                });
            }
        }
    }

    private static void WriteTripDaysCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountTripExportData> trips)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "trip-days.csv");
        WriteCsvRow(writer, new[]
        {
            "tripReference", "localDate", "parkName", "isParkAvailable", "desiredArrivalTime", "groupNote",
        });
        foreach (AccountTripExportData data in trips)
        {
            foreach (TripExportDayResult day in data.Trip.Days)
            {
                WriteCsvRow(writer, new[]
                {
                    data.Reference, FormatDate(day.LocalDate), day.ParkName, Boolean(day.IsParkAvailable),
                    FormatTime(day.DesiredArrivalTime), day.GroupNote,
                });
            }
        }
    }

    private static void WriteTripDayBlocksCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountTripExportData> trips)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "trip-day-blocks.csv");
        WriteCsvRow(writer, new[]
        {
            "tripReference", "localDate", "type", "title", "details", "localTime",
        });
        foreach (AccountTripExportData data in trips)
        {
            foreach (TripExportDayResult day in data.Trip.Days)
            {
                foreach (TripExportDayBlockResult block in day.Blocks)
                {
                    WriteCsvRow(writer, new[]
                    {
                        data.Reference, FormatDate(day.LocalDate), block.Type.ToString(), block.Title,
                        block.Details, FormatTime(block.LocalTime),
                    });
                }
            }
        }
    }

    private static void WriteTripDecisionsCsv(
        ZipArchive archive,
        IReadOnlyCollection<AccountTripExportData> trips)
    {
        using StreamWriter writer = CreateCsvWriter(archive, "trip-decisions.csv");
        WriteCsvRow(writer, new[]
        {
            "tripReference", "parkName", "parkItemName", "isParkItemAvailable", "status", "reason",
            "decidedAtUtc",
        });
        foreach (AccountTripExportData data in trips)
        {
            foreach (TripExportDecisionResult decision in data.Trip.CollectiveDecisions)
            {
                WriteCsvRow(writer, new[]
                {
                    data.Reference, decision.ParkName, decision.ParkItemName,
                    Boolean(decision.IsParkItemAvailable), decision.Status.ToString(), decision.Reason,
                    FormatUtc(decision.DecidedAtUtc),
                });
            }
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

    private static void WriteNullableDate(Utf8JsonWriter writer, string name, DateOnly? value)
    {
        WriteNullableString(writer, name, Date(value));
    }

    private static string FormatUtc(DateTime value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static string FormatDate(DateOnly value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string? Date(DateOnly? value)
    {
        return value.HasValue ? FormatDate(value.Value) : null;
    }

    private static string? FormatTime(TimeOnly? value)
    {
        return value?.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static string Boolean(bool value)
    {
        return value ? "true" : "false";
    }

    private static string? Integer(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }
}
