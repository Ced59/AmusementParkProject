using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using Microsoft.AspNetCore.WebUtilities;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportRideOccurrenceCursorCodec
{
    private const int CurrentVersion = 1;
    private const int MaximumEncodedLength = 512;

    public static string Encode(RideOccurrenceListCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        CursorPayload payload = new CursorPayload(
            CurrentVersion,
            cursor.SortPosition,
            cursor.CreatedAtUtc,
            cursor.OccurrenceId.Value);
        return WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
    }

    public static bool TryDecode(
        string? encodedCursor,
        out RideOccurrenceListCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            return true;
        }

        string normalized = encodedCursor.Trim();
        if (normalized.Length > MaximumEncodedLength)
        {
            return false;
        }

        try
        {
            CursorPayload? payload = JsonSerializer.Deserialize<CursorPayload>(
                WebEncoders.Base64UrlDecode(normalized));
            if (payload is null
                || payload.Version != CurrentVersion
                || payload.CreatedAtUtc.Kind != DateTimeKind.Utc)
            {
                return false;
            }

            cursor = new RideOccurrenceListCursor(
                payload.SortPosition,
                payload.CreatedAtUtc,
                RideOccurrenceId.Parse(payload.OccurrenceId));
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
        long SortPosition,
        DateTime CreatedAtUtc,
        string OccurrenceId);
}
