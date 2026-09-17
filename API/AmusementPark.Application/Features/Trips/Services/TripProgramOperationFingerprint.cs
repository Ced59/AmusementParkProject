using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

internal static class TripProgramOperationFingerprint
{
    public static string BuildOperationId(string userId, TripPlanId tripPlanId, string idempotencyKey)
    {
        return HashFields(new[] { userId, tripPlanId.Value, idempotencyKey });
    }

    public static string BuildCandidateRequestHash(TripParkCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return BuildCandidateRequestHash(
            candidate.ParkId,
            candidate.CandidateDates,
            candidate.Source,
            candidate.CollectiveNote);
    }

    public static string BuildCandidateRequestHash(
        string parkId,
        IReadOnlyCollection<DateOnly> candidateDates,
        TripParkCandidateSource source,
        string? collectiveNote)
    {
        ArgumentNullException.ThrowIfNull(candidateDates);
        DateOnly[] normalizedDates = candidateDates.Distinct().OrderBy(static date => date).ToArray();
        List<string?> fields = new()
        {
            parkId.Trim(),
            normalizedDates.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        fields.AddRange(normalizedDates.Select(static date => date.ToString("yyyy-MM-dd")));
        fields.Add(source.ToString());
        fields.Add(NormalizeOptional(collectiveNote));
        return HashFields(fields);
    }

    public static string BuildDayRequestHash(DateOnly localDate, TripDayPlanInput input)
    {
        List<string?> fields = new()
        {
            localDate.ToString("yyyy-MM-dd"),
            input.ParkCandidateId?.Trim() ?? string.Empty,
            input.DesiredArrivalTime?.ToString("HH:mm"),
            NormalizeOptional(input.GroupNote),
            input.Blocks.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        foreach (TripDayBlockInput block in input.Blocks
            .OrderBy(static item => item.SortPosition)
            .ThenBy(static item => item.BlockId, StringComparer.Ordinal))
        {
            fields.Add(block.BlockId?.Trim());
            fields.Add(block.Type.ToString());
            fields.Add(block.Title?.Trim() ?? string.Empty);
            fields.Add(NormalizeOptional(block.Details));
            fields.Add(block.LocalTime?.ToString("HH:mm"));
            fields.Add(block.SortPosition.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return HashFields(fields);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string HashFields(IEnumerable<string?> fields)
    {
        StringBuilder serialized = new();
        foreach (string? field in fields)
        {
            string value = field ?? string.Empty;
            serialized.Append(value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            serialized.Append(':');
            serialized.Append(value);
            serialized.Append(';');
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(serialized.ToString()));
        return Convert.ToHexStringLower(digest);
    }
}
