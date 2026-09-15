using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.FactualEvents;

public static class OpeningCalendarFactSnapshot
{
    private const int MaximumPresentedWindowCount = 24;

    public static FactValue? Create(ParkOpeningHoursSchedule? schedule)
    {
        if (schedule is null)
        {
            return null;
        }

        if (schedule.RegularRules.Count == 0 && schedule.DateOverrides.Count == 0)
        {
            return null;
        }

        string timeZoneId = schedule.TimeZoneId?.Trim() ?? string.Empty;
        if (timeZoneId.Length == 0)
        {
            return null;
        }

        Dictionary<(int SortOrder, DateOnly StartDate), int> tieOrders =
            new Dictionary<(int SortOrder, DateOnly StartDate), int>();
        List<string> canonicalRules = new List<string>();
        foreach (ParkOpeningHoursRule rule in schedule.RegularRules)
        {
            (int SortOrder, DateOnly StartDate) key = (rule.SortOrder, rule.StartDate);
            _ = tieOrders.TryGetValue(key, out int tieOrder);
            canonicalRules.Add(BuildCanonicalRule(rule, tieOrder));
            tieOrders[key] = tieOrder + 1;
        }

        canonicalRules.Sort(StringComparer.Ordinal);
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
        IReadOnlyCollection<string> openingWindows = BuildOpeningWindows(schedule);
        string presentedWindows = string.Join(",", openingWindows.Take(MaximumPresentedWindowCount));
        return FactValue.FromText(
            $"timezone={timeZoneId};coverage={coverage};rules={canonicalRules.Count};overrides={canonicalOverrides.Count};windows={presentedWindows};windowCount={openingWindows.Count};sha256={hashText}");
    }

    private static IReadOnlyCollection<string> BuildOpeningWindows(
        ParkOpeningHoursSchedule schedule)
    {
        IEnumerable<ParkOpeningHoursTimeRange> regularWindows = schedule.RegularRules
            .Where(static rule => !rule.IsClosed)
            .SelectMany(static rule => rule.TimeRanges);
        IEnumerable<ParkOpeningHoursTimeRange> overrideWindows = schedule.DateOverrides
            .Where(static dateOverride => !dateOverride.IsClosed)
            .SelectMany(static dateOverride => dateOverride.TimeRanges);
        return regularWindows
            .Concat(overrideWindows)
            .Select(BuildPresentedWindow)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildPresentedWindow(ParkOpeningHoursTimeRange range)
    {
        string closesNextDay = range.ClosesNextDay ? "+1" : string.Empty;
        string lastAdmission = range.LastAdmissionAt.HasValue
            ? $"@{range.LastAdmissionAt.Value.ToString("HH:mm", CultureInfo.InvariantCulture)}{(range.LastAdmissionNextDay ? "+1" : string.Empty)}"
            : string.Empty;
        return $"{range.OpensAt.ToString("HH:mm", CultureInfo.InvariantCulture)}-{range.ClosesAt.ToString("HH:mm", CultureInfo.InvariantCulture)}{closesNextDay}{lastAdmission}";
    }

    private static string BuildCanonicalRule(
        ParkOpeningHoursRule rule,
        int tieOrder)
    {
        StringBuilder value = new StringBuilder();
        AppendField(value, rule.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendField(value, rule.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AppendField(value, rule.IsClosed ? "1" : "0");
        AppendField(value, rule.SortOrder.ToString(CultureInfo.InvariantCulture));
        AppendField(value, tieOrder.ToString(CultureInfo.InvariantCulture));
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
