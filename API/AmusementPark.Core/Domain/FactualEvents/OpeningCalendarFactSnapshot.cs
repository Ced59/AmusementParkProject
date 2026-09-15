using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.FactualEvents;

public static class OpeningCalendarFactSnapshot
{
    private const int MaximumPresentedEntryCount = 16;
    private const int MaximumPresentedWindowCountPerEntry = 6;
    private const string SnapshotVersion = "2";

    public static FactValue? Create(ParkOpeningHoursSchedule? schedule)
    {
        return CreateSnapshot(schedule, null, 0);
    }

    public static (FactValue? PreviousValue, FactValue? NewValue) CreateChange(
        ParkOpeningHoursSchedule? previousSchedule,
        ParkOpeningHoursSchedule? currentSchedule)
    {
        IReadOnlyCollection<string> previousEntries = previousSchedule is null
            ? Array.Empty<string>()
            : BuildPresentedEntries(previousSchedule);
        IReadOnlyCollection<string> currentEntries = currentSchedule is null
            ? Array.Empty<string>()
            : BuildPresentedEntries(currentSchedule);
        (IReadOnlyCollection<string> Ordered, int ChangedCount) previousPresentation =
            PrioritizeChangedEntries(previousEntries, currentEntries);
        (IReadOnlyCollection<string> Ordered, int ChangedCount) currentPresentation =
            PrioritizeChangedEntries(currentEntries, previousEntries);
        return (
            CreateSnapshot(
                previousSchedule,
                previousPresentation.Ordered,
                previousPresentation.ChangedCount),
            CreateSnapshot(
                currentSchedule,
                currentPresentation.Ordered,
                currentPresentation.ChangedCount));
    }

    private static FactValue? CreateSnapshot(
        ParkOpeningHoursSchedule? schedule,
        IReadOnlyCollection<string>? presentationEntries,
        int changedEntryCount)
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
        IReadOnlyCollection<string> entries = BuildPresentedEntries(schedule);
        IReadOnlyCollection<string> orderedPresentationEntries = presentationEntries ?? entries;
        return FactValue.FromText(
            $"snapshot={SnapshotVersion};timezone={timeZoneId};coverage={coverage};rules={canonicalRules.Count};overrides={canonicalOverrides.Count};evidence=complete;entries={string.Join("~", orderedPresentationEntries.Take(MaximumPresentedEntryCount))};entryCount={entries.Count};changedEntryCount={changedEntryCount};sha256={hashText}");
    }

    public static FactValue MigrateLegacy(
        FactValue value,
        ParkOpeningHoursSchedule? currentSchedule)
    {
        ArgumentNullException.ThrowIfNull(value);
        IReadOnlyDictionary<string, string> fields = ParseFields(value.CanonicalValue);
        if (fields.TryGetValue("snapshot", out string? version)
            && string.Equals(version, SnapshotVersion, StringComparison.Ordinal))
        {
            return value;
        }

        if (!fields.TryGetValue("sha256", out string? legacyHash)
            || !fields.ContainsKey("timezone")
            || !fields.ContainsKey("rules")
            || !fields.ContainsKey("overrides"))
        {
            return value;
        }

        FactValue? currentSnapshot = Create(currentSchedule);
        if (currentSnapshot is not null
            && string.Equals(
                ExtractField(currentSnapshot.CanonicalValue, "sha256"),
                legacyHash,
                StringComparison.Ordinal))
        {
            return currentSnapshot;
        }

        string coverage = fields.TryGetValue("coverage", out string? coverageValue)
            ? coverageValue
            : "none";
        return FactValue.FromText(
            $"snapshot={SnapshotVersion};timezone={fields["timezone"]};coverage={coverage};rules={fields["rules"]};overrides={fields["overrides"]};evidence=unavailable;entries=;entryCount=0;changedEntryCount=0;sha256={legacyHash}");
    }

    private static (IReadOnlyCollection<string> Ordered, int ChangedCount) PrioritizeChangedEntries(
        IReadOnlyCollection<string> entries,
        IReadOnlyCollection<string> counterpartEntries)
    {
        Dictionary<string, int> counterpartCounts = counterpartEntries
            .GroupBy(static entry => entry, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Count(),
                StringComparer.Ordinal);
        List<string> changedEntries = new List<string>();
        List<string> unchangedEntries = new List<string>();
        foreach (string entry in entries)
        {
            if (counterpartCounts.TryGetValue(entry, out int remainingCount)
                && remainingCount > 0)
            {
                counterpartCounts[entry] = remainingCount - 1;
                unchangedEntries.Add(entry);
            }
            else
            {
                changedEntries.Add(entry);
            }
        }

        return (
            changedEntries.Concat(unchangedEntries).ToArray(),
            changedEntries.Count);
    }

    private static IReadOnlyCollection<string> BuildPresentedEntries(
        ParkOpeningHoursSchedule schedule)
    {
        Dictionary<(int SortOrder, DateOnly StartDate), int> tieOrders =
            new Dictionary<(int SortOrder, DateOnly StartDate), int>();
        List<string> regularEntries = new List<string>();
        foreach (ParkOpeningHoursRule rule in schedule.RegularRules)
        {
            (int SortOrder, DateOnly StartDate) key = (rule.SortOrder, rule.StartDate);
            _ = tieOrders.TryGetValue(key, out int tieOrder);
            regularEntries.Add(BuildPresentedRule(rule, tieOrder));
            tieOrders[key] = tieOrder + 1;
        }

        IEnumerable<string> overrideEntries = schedule.DateOverrides
            .Select(BuildPresentedOverride);
        return regularEntries
            .Concat(overrideEntries)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildPresentedRule(ParkOpeningHoursRule rule, int tieOrder)
    {
        string days = string.Join(",", rule.DaysOfWeek
            .Distinct()
            .OrderBy(static day => day)
            .Select(static day => ((int)day).ToString(CultureInfo.InvariantCulture)));
        return BuildPresentedEntry(
            "R",
            rule.StartDate,
            rule.EndDate,
            days,
            rule.IsClosed,
            rule.SortOrder.ToString(CultureInfo.InvariantCulture),
            rule.TimeRanges,
            tieOrder.ToString(CultureInfo.InvariantCulture));
    }

    private static string BuildPresentedOverride(ParkOpeningHoursDateOverride dateOverride)
    {
        return BuildPresentedEntry(
            "D",
            dateOverride.LocalDate,
            dateOverride.LocalDate,
            string.Empty,
            dateOverride.IsClosed,
            string.Empty,
            dateOverride.TimeRanges,
            string.Empty);
    }

    private static string BuildPresentedEntry(
        string kind,
        DateOnly startDate,
        DateOnly endDate,
        string days,
        bool isClosed,
        string priority,
        IReadOnlyCollection<ParkOpeningHoursTimeRange> ranges,
        string tieOrder)
    {
        string windows = string.Join(",", ranges
            .Select(BuildPresentedWindow)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .Take(MaximumPresentedWindowCountPerEntry));
        return string.Join(
            "|",
            kind,
            startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            days,
            isClosed ? "C" : "O",
            priority,
            windows,
            ranges.Count.ToString(CultureInfo.InvariantCulture),
            tieOrder);
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

    private static IReadOnlyDictionary<string, string> ParseFields(string canonicalValue)
    {
        Dictionary<string, string> fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string part in canonicalValue.Split(';'))
        {
            int separator = part.IndexOf('=');
            if (separator > 0)
            {
                fields[part[..separator]] = part[(separator + 1)..];
            }
        }

        return fields;
    }

    private static string? ExtractField(string canonicalValue, string fieldName)
    {
        IReadOnlyDictionary<string, string> fields = ParseFields(canonicalValue);
        return fields.TryGetValue(fieldName, out string? value) ? value : null;
    }
}
