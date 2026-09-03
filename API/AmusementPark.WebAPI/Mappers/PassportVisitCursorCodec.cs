using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using Microsoft.AspNetCore.WebUtilities;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportVisitCursorCodec
{
    private const int CurrentVersion = 1;
    private const int MaximumEncodedLength = 512;

    public static string Encode(UserVisitListCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);

        CursorPayload payload = new CursorPayload(
            CurrentVersion,
            cursor.Date.Year,
            cursor.Date.Month,
            cursor.Date.Day,
            cursor.Date.Precision,
            cursor.Date.IsApproximate,
            cursor.UpdatedAtUtc,
            cursor.VisitId.Value);
        return WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
    }

    public static bool TryDecode(string? encodedCursor, out UserVisitListCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            return true;
        }

        string normalizedCursor = encodedCursor.Trim();
        if (normalizedCursor.Length > MaximumEncodedLength)
        {
            return false;
        }

        try
        {
            byte[] json = WebEncoders.Base64UrlDecode(normalizedCursor);
            CursorPayload? payload = JsonSerializer.Deserialize<CursorPayload>(json);
            if (payload is null
                || payload.Version != CurrentVersion
                || payload.UpdatedAtUtc.Kind != DateTimeKind.Utc)
            {
                return false;
            }

            VisitDate date = new VisitDate(
                payload.Year,
                payload.Month,
                payload.Day,
                payload.Precision,
                payload.IsApproximate);
            cursor = new UserVisitListCursor(
                date,
                payload.UpdatedAtUtc,
                VisitId.Parse(payload.VisitId));
            return true;
        }
        catch (Exception exception) when (
            exception is FormatException
            or JsonException
            or ArgumentException)
        {
            return false;
        }
    }

    private sealed record CursorPayload(
        int Version,
        int Year,
        int? Month,
        int? Day,
        VisitDatePrecision Precision,
        bool IsApproximate,
        DateTime UpdatedAtUtc,
        string VisitId);
}
