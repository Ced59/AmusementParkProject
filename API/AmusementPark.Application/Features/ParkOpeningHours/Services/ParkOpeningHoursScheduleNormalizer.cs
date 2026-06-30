using AmusementPark.Application.Errors;
using AmusementPark.Core.Localization;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursScheduleNormalizer
{
    private const int MaximumRegularRuleCount = 250;
    private const int MaximumDateOverrideCount = 1500;
    private const int MaximumTimeRangeCount = 8;

    public ApplicationResult<ParkOpeningHoursSchedule> Normalize(ParkOpeningHoursSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        Dictionary<string, IReadOnlyCollection<string>> errors = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
        ParkOpeningHoursSchedule normalized = new ParkOpeningHoursSchedule
        {
            Id = NormalizeOptionalString(schedule.Id),
            ParkId = NormalizeOptionalString(schedule.ParkId) ?? string.Empty,
            TimeZoneId = NormalizeOptionalString(schedule.TimeZoneId) ?? string.Empty,
            SourceUrl = NormalizeOptionalString(schedule.SourceUrl),
            Notes = NormalizeOptionalString(schedule.Notes),
            LastVerifiedAtUtc = schedule.LastVerifiedAtUtc,
            CreatedAtUtc = schedule.CreatedAtUtc,
            UpdatedAtUtc = schedule.UpdatedAtUtc,
        };

        if (string.IsNullOrWhiteSpace(normalized.ParkId))
        {
            errors[nameof(schedule.ParkId)] = new[] { "required" };
        }

        if (string.IsNullOrWhiteSpace(normalized.TimeZoneId))
        {
            errors[nameof(schedule.TimeZoneId)] = new[] { "required" };
        }
        else if (!IsValidTimeZone(normalized.TimeZoneId))
        {
            errors[nameof(schedule.TimeZoneId)] = new[] { "invalid-time-zone" };
        }

        if (schedule.RegularRules.Count > MaximumRegularRuleCount)
        {
            errors[nameof(schedule.RegularRules)] = new[] { "too-many-rules" };
        }

        if (schedule.DateOverrides.Count > MaximumDateOverrideCount)
        {
            errors[nameof(schedule.DateOverrides)] = new[] { "too-many-overrides" };
        }

        normalized.RegularRules = NormalizeRules(schedule.RegularRules, errors);
        normalized.DateOverrides = NormalizeDateOverrides(schedule.DateOverrides, errors);

        if (errors.Count > 0)
        {
            return ApplicationResult<ParkOpeningHoursSchedule>.Failure(ParkOpeningHoursApplicationErrors.InvalidSchedule(errors));
        }

        return ApplicationResult<ParkOpeningHoursSchedule>.Success(normalized);
    }

    private static List<ParkOpeningHoursRule> NormalizeRules(
        IReadOnlyCollection<ParkOpeningHoursRule> rules,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        List<ParkOpeningHoursRule> normalizedRules = new List<ParkOpeningHoursRule>();
        int index = 0;
        foreach (ParkOpeningHoursRule rule in rules)
        {
            string fieldPrefix = $"{nameof(ParkOpeningHoursSchedule.RegularRules)}[{index}]";
            ParkOpeningHoursRule normalizedRule = new ParkOpeningHoursRule
            {
                Id = NormalizeOptionalString(rule.Id) ?? Guid.NewGuid().ToString("N"),
                StartDate = rule.StartDate,
                EndDate = rule.EndDate,
                DaysOfWeek = rule.DaysOfWeek.Distinct().OrderBy(static day => day).ToList(),
                IsClosed = rule.IsClosed,
                Labels = NormalizeLocalizedTexts(rule.Labels),
                Reasons = NormalizeLocalizedTexts(rule.Reasons),
                SortOrder = rule.SortOrder > 0 ? rule.SortOrder : index + 1,
            };

            if (normalizedRule.StartDate == default || normalizedRule.EndDate == default || normalizedRule.StartDate > normalizedRule.EndDate)
            {
                errors[$"{fieldPrefix}.dateRange"] = new[] { "invalid-date-range" };
            }

            if (normalizedRule.DaysOfWeek.Count == 0)
            {
                errors[$"{fieldPrefix}.daysOfWeek"] = new[] { "required" };
            }

            normalizedRule.TimeRanges = normalizedRule.IsClosed
                ? new List<ParkOpeningHoursTimeRange>()
                : NormalizeTimeRanges(rule.TimeRanges, $"{fieldPrefix}.timeRanges", errors);

            if (!normalizedRule.IsClosed && normalizedRule.TimeRanges.Count == 0)
            {
                errors[$"{fieldPrefix}.timeRanges"] = new[] { "required" };
            }

            normalizedRules.Add(normalizedRule);
            index += 1;
        }

        return normalizedRules
            .OrderBy(static rule => rule.SortOrder)
            .ThenBy(static rule => rule.StartDate)
            .ToList();
    }

    private static List<ParkOpeningHoursDateOverride> NormalizeDateOverrides(
        IReadOnlyCollection<ParkOpeningHoursDateOverride> overrides,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        List<ParkOpeningHoursDateOverride> normalizedOverrides = new List<ParkOpeningHoursDateOverride>();
        HashSet<DateOnly> usedDates = new HashSet<DateOnly>();
        int index = 0;
        foreach (ParkOpeningHoursDateOverride dateOverride in overrides)
        {
            string fieldPrefix = $"{nameof(ParkOpeningHoursSchedule.DateOverrides)}[{index}]";
            ParkOpeningHoursDateOverride normalizedOverride = new ParkOpeningHoursDateOverride
            {
                LocalDate = dateOverride.LocalDate,
                IsClosed = dateOverride.IsClosed,
                Labels = NormalizeLocalizedTexts(dateOverride.Labels),
                Reasons = NormalizeLocalizedTexts(dateOverride.Reasons),
            };

            if (normalizedOverride.LocalDate == default)
            {
                errors[$"{fieldPrefix}.localDate"] = new[] { "required" };
            }
            else if (!usedDates.Add(normalizedOverride.LocalDate))
            {
                errors[$"{fieldPrefix}.localDate"] = new[] { "duplicate" };
            }

            normalizedOverride.TimeRanges = normalizedOverride.IsClosed
                ? new List<ParkOpeningHoursTimeRange>()
                : NormalizeTimeRanges(dateOverride.TimeRanges, $"{fieldPrefix}.timeRanges", errors);

            if (!normalizedOverride.IsClosed && normalizedOverride.TimeRanges.Count == 0)
            {
                errors[$"{fieldPrefix}.timeRanges"] = new[] { "required" };
            }

            normalizedOverrides.Add(normalizedOverride);
            index += 1;
        }

        return normalizedOverrides
            .OrderBy(static dateOverride => dateOverride.LocalDate)
            .ToList();
    }

    private static List<ParkOpeningHoursTimeRange> NormalizeTimeRanges(
        IReadOnlyCollection<ParkOpeningHoursTimeRange> timeRanges,
        string fieldPrefix,
        Dictionary<string, IReadOnlyCollection<string>> errors)
    {
        if (timeRanges.Count > MaximumTimeRangeCount)
        {
            errors[fieldPrefix] = new[] { "too-many-time-ranges" };
        }

        List<ParkOpeningHoursTimeRange> normalizedTimeRanges = new List<ParkOpeningHoursTimeRange>();
        int index = 0;
        foreach (ParkOpeningHoursTimeRange timeRange in timeRanges)
        {
            string rangePrefix = $"{fieldPrefix}[{index}]";
            ParkOpeningHoursTimeRange normalizedTimeRange = new ParkOpeningHoursTimeRange
            {
                OpensAt = timeRange.OpensAt,
                ClosesAt = timeRange.ClosesAt,
                ClosesNextDay = timeRange.ClosesNextDay,
                LastAdmissionAt = timeRange.LastAdmissionAt,
                LastAdmissionNextDay = timeRange.LastAdmissionNextDay,
            };

            if (!IsValidRange(normalizedTimeRange))
            {
                errors[rangePrefix] = new[] { "invalid-time-range" };
            }

            if (!IsValidLastAdmission(normalizedTimeRange))
            {
                errors[$"{rangePrefix}.lastAdmissionAt"] = new[] { "outside-time-range" };
            }

            normalizedTimeRanges.Add(normalizedTimeRange);
            index += 1;
        }

        return normalizedTimeRanges
            .OrderBy(static timeRange => timeRange.OpensAt)
            .ThenBy(static timeRange => timeRange.ClosesNextDay)
            .ThenBy(static timeRange => timeRange.ClosesAt)
            .ToList();
    }

    private static bool IsValidRange(ParkOpeningHoursTimeRange timeRange)
    {
        int opensAtMinutes = ToAbsoluteMinutes(timeRange.OpensAt, false);
        int closesAtMinutes = ToAbsoluteMinutes(timeRange.ClosesAt, timeRange.ClosesNextDay);
        return closesAtMinutes > opensAtMinutes;
    }

    private static bool IsValidLastAdmission(ParkOpeningHoursTimeRange timeRange)
    {
        if (!timeRange.LastAdmissionAt.HasValue)
        {
            return true;
        }

        int opensAtMinutes = ToAbsoluteMinutes(timeRange.OpensAt, false);
        int closesAtMinutes = ToAbsoluteMinutes(timeRange.ClosesAt, timeRange.ClosesNextDay);
        int lastAdmissionAtMinutes = ToAbsoluteMinutes(timeRange.LastAdmissionAt.Value, timeRange.LastAdmissionNextDay);
        return lastAdmissionAtMinutes >= opensAtMinutes && lastAdmissionAtMinutes <= closesAtMinutes;
    }

    private static int ToAbsoluteMinutes(TimeOnly time, bool nextDay)
    {
        int minutes = (time.Hour * 60) + time.Minute;
        return nextDay ? minutes + (24 * 60) : minutes;
    }

    private static bool IsValidTimeZone(string timeZoneId)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out string? windowsTimeZoneId)
            && !string.IsNullOrWhiteSpace(windowsTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsTimeZoneId, out _))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out string? ianaTimeZoneId)
            && !string.IsNullOrWhiteSpace(ianaTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(ianaTimeZoneId, out _))
        {
            return true;
        }

        return false;
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static List<LocalizedText> NormalizeLocalizedTexts(IReadOnlyCollection<LocalizedText>? values)
    {
        Dictionary<string, LocalizedText> result = new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);
        if (values is null)
        {
            return new List<LocalizedText>();
        }

        foreach (LocalizedText value in values)
        {
            string languageCode = NormalizeOptionalString(value.LanguageCode)?.ToLowerInvariant() ?? string.Empty;
            string text = NormalizeOptionalString(value.Value) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            result[languageCode] = new LocalizedText(languageCode, text);
        }

        return result.Values
            .OrderBy(static value => value.LanguageCode)
            .ToList();
    }
}
