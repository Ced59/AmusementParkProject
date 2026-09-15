using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.FactualEvents;

public static class OpeningCalendarFactSnapshot
{
    public static FactValue? Create(ParkOpeningHoursSchedule? schedule)
    {
        if (schedule is null)
        {
            return null;
        }

        string timeZoneId = schedule.TimeZoneId?.Trim() ?? string.Empty;
        if (timeZoneId.Length == 0)
        {
            return null;
        }

        List<string> canonicalRules = schedule.RegularRules
            .Select(BuildCanonicalRule)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToList();
        List<string> canonicalOverrides = schedule.DateOverrides
            .Select(BuildCanonicalOverride)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToList();
        StringBuilder canonical = new StringBuilder();
        AppendField(canonical, timeZoneId);
        foreach (string rule in canonicalRules)
        {
            AppendField(canonical, rule);
        }

        canonical.Append('|');
        foreach (string dateOverride in canonicalOverrides)
        {
            AppendField(canonical, dateOverride);
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        string hashText = Convert.ToHexString(hash).ToLowerInvariant();
        string coverage = BuildCoverage(schedule);
        return FactValue.FromText(
            $"timezone={timeZoneId};coverage={coverage};rules={canonicalRules.Count};overrides={canonicalOverrides.Count};sha256={hashText}");
    }

    private static string BuildCanonicalRule(ParkOpeningHoursRule rule)
    {
        StringBuilder value = new StringBuilder();
        AppendField(value, rule.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendField(value, rule.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendField(value, rule.IsClosed ? "1" : "0");
        AppendField(
            value,
            string.Join(",", rule.DaysOfWeek.OrderBy(static day => day).Select(static day => (int)day)));
        AppendTimeRanges(value, rule.TimeRanges);
        return value.ToString();
    }

    private static string BuildCanonicalOverride(ParkOpeningHoursDateOverride dateOverride)
    {
        StringBuilder value = new StringBuilder();
        AppendField(value, dateOverride.LocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendField(value, dateOverride.IsClosed ? "1" : "0");
        AppendTimeRanges(value, dateOverride.TimeRanges);
        return value.ToString();
    }

    private static void AppendTimeRanges(
        StringBuilder target,
        IReadOnlyCollection<ParkOpeningHoursTimeRange> ranges)
    {
        IEnumerable<string> canonicalRanges = ranges
            .Select(static range => string.Join(
                ",",
                range.OpensAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                range.ClosesAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                range.ClosesNextDay ? "1" : "0",
                range.LastAdmissionAt?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty,
                range.LastAdmissionNextDay ? "1" : "0"))
            .OrderBy(static value => value, StringComparer.Ordinal);
        AppendField(target, string.Join("|", canonicalRanges));
    }

    private static string BuildCoverage(ParkOpeningHoursSchedule schedule)
    {
        IEnumerable<DateOnly> startDates = schedule.RegularRules
            .Select(static rule => rule.StartDate)
            .Concat(schedule.DateOverrides.Select(static value => value.LocalDate))
            .Where(static value => value != default);
        IEnumerable<DateOnly> endDates = schedule.RegularRules
            .Select(static rule => rule.EndDate)
            .Concat(schedule.DateOverrides.Select(static value => value.LocalDate))
            .Where(static value => value != default);
        DateOnly? firstDate = startDates.Select(static value => (DateOnly?)value).Min();
        DateOnly? lastDate = endDates.Select(static value => (DateOnly?)value).Max();
        if (!firstDate.HasValue || !lastDate.HasValue)
        {
            return "none";
        }

        return $"{firstDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/{lastDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
    }

    private static void AppendField(StringBuilder target, string value)
    {
        target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        target.Append(':');
        target.Append(value);
    }
}
