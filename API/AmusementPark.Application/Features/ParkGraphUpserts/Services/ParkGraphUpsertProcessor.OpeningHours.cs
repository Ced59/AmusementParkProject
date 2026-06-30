using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private const string OpeningHoursEntityType = "ParkOpeningHours";
    private const string OpeningHoursPropertyName = "openingHours";
    private const string LegacyOpeningHoursPropertyName = "parkOpeningHours";
    private const string OpeningHoursDateFormat = "yyyy-MM-dd";
    private const string OpeningHoursTimeFormat = "HH:mm";

    private async Task ProcessOpeningHoursAsync(JsonElement root, Park targetPark, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!HasOpeningHoursPatch(root))
        {
            return;
        }

        JsonElement? patch = ResolveOpeningHoursPatch(root);
        ParkGraphUpsertChange change = BuildEntityChange(
            OpeningHoursEntityType,
            targetPark.Id,
            OpeningHoursPropertyName,
            string.IsNullOrWhiteSpace(targetPark.Name) ? targetPark.Id : $"{targetPark.Name} opening hours",
            "Unchanged",
            OpeningHoursPropertyName);

        if (patch is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("openingHours doit être un objet JSON.");
            return;
        }

        if (!HasOpeningHoursScheduleData(patch.Value))
        {
            return;
        }

        if (this.parkOpeningHoursRepository is null
            || this.parkOpeningHoursScheduleNormalizer is null
            || this.parkOpeningHoursCoverageSegmentBuilder is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("Le traitement des horaires n'est pas disponible dans ce contexte.");
            return;
        }

        List<string> readErrors = new List<string>();
        ParkOpeningHoursSchedule schedule = ReadOpeningHoursSchedule(patch.Value, targetPark.Id, readErrors);
        string? requestedParkId = ReadString(patch, "parkId");
        if (!string.IsNullOrWhiteSpace(requestedParkId) && !string.Equals(requestedParkId, targetPark.Id, StringComparison.Ordinal))
        {
            readErrors.Add($"openingHours.parkId pointe vers '{requestedParkId}' mais le parc cible est '{targetPark.Id}'.");
        }

        if (readErrors.Count > 0)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.AddRange(readErrors);
            return;
        }

        ApplicationResult<ParkOpeningHoursSchedule> normalizedResult = this.parkOpeningHoursScheduleNormalizer.Normalize(schedule);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            AddOpeningHoursValidationErrors(result, normalizedResult);
            return;
        }

        ParkOpeningHoursSchedule normalizedSchedule = normalizedResult.Value;
        ParkOpeningHoursSchedule? existingSchedule = await this.parkOpeningHoursRepository.GetByParkIdAsync(targetPark.Id, cancellationToken);
        bool isNew = existingSchedule is null || !HasOpeningHoursData(existingSchedule);

        AddOpeningHoursChanges(change, existingSchedule, normalizedSchedule);
        if (change.Fields.Count > 0 || isNew)
        {
            change.ChangeType = isNew ? "Created" : "Updated";
        }

        result.Changes.Add(change);
        if (!apply)
        {
            return;
        }

        normalizedSchedule.CoverageSegments = this.parkOpeningHoursCoverageSegmentBuilder.BuildSegments(normalizedSchedule).ToList();
        await this.parkOpeningHoursRepository.UpsertAsync(normalizedSchedule, cancellationToken);
    }

    private static bool HasOpeningHoursPatch(JsonElement root)
    {
        return HasProperty(root, OpeningHoursPropertyName) || HasProperty(root, LegacyOpeningHoursPropertyName);
    }

    private static JsonElement? ResolveOpeningHoursPatch(JsonElement? root)
    {
        return GetObject(root, OpeningHoursPropertyName) ?? GetObject(root, LegacyOpeningHoursPropertyName);
    }

    private static bool HasOpeningHoursScheduleData(JsonElement patch)
    {
        return HasNonEmptyArrayValue(patch, "regularRules") || HasNonEmptyArrayValue(patch, "dateOverrides");
    }

    private static bool HasNonEmptyArrayValue(JsonElement patch, string propertyName)
    {
        if (!patch.TryGetProperty(propertyName, out JsonElement value))
        {
            return false;
        }

        return value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 0;
    }

    private static ParkOpeningHoursSchedule ReadOpeningHoursSchedule(JsonElement patch, string targetParkId, List<string> errors)
    {
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = targetParkId,
            TimeZoneId = ReadString(patch, "timeZoneId") ?? string.Empty,
            SourceUrl = ReadString(patch, "sourceUrl"),
            Notes = ReadString(patch, "notes"),
            LastVerifiedAtUtc = ReadDate(patch, "lastVerifiedAtUtc"),
            RegularRules = ReadOpeningHoursRules(patch, errors),
            DateOverrides = ReadOpeningHoursDateOverrides(patch, errors),
        };

        return schedule;
    }

    private static List<ParkOpeningHoursRule> ReadOpeningHoursRules(JsonElement patch, List<string> errors)
    {
        List<ParkOpeningHoursRule> rules = new List<ParkOpeningHoursRule>();
        JsonElement? rulesArray = GetArray(patch, "regularRules");
        if (rulesArray is null)
        {
            if (HasProperty(patch, "regularRules"))
            {
                errors.Add("openingHours.regularRules doit être un tableau.");
            }

            return rules;
        }

        int index = 0;
        foreach (JsonElement ruleElement in rulesArray.Value.EnumerateArray())
        {
            string prefix = $"openingHours.regularRules[{index}]";
            if (ruleElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{prefix} doit être un objet.");
                index += 1;
                continue;
            }

            AddLegacyLocalizedOpeningHoursErrors(ruleElement, prefix, errors);

            ParkOpeningHoursRule rule = new ParkOpeningHoursRule
            {
                Id = ReadString(ruleElement, "id"),
                StartDate = ReadDateOnly(ruleElement, "startDate", $"{prefix}.startDate", errors),
                EndDate = ReadDateOnly(ruleElement, "endDate", $"{prefix}.endDate", errors),
                DaysOfWeek = ReadDaysOfWeek(ruleElement, $"{prefix}.daysOfWeek", errors),
                IsClosed = ReadBool(ruleElement, "isClosed") ?? false,
                Labels = ReadLocalizedTexts(GetArray(ruleElement, "labels")),
                Reasons = ReadLocalizedTexts(GetArray(ruleElement, "reasons")),
                SortOrder = ReadInt(ruleElement, "sortOrder") ?? index + 1,
                TimeRanges = ReadOpeningHoursTimeRanges(ruleElement, $"{prefix}.timeRanges", errors),
            };

            rules.Add(rule);
            index += 1;
        }

        return rules;
    }

    private static List<ParkOpeningHoursDateOverride> ReadOpeningHoursDateOverrides(JsonElement patch, List<string> errors)
    {
        List<ParkOpeningHoursDateOverride> overrides = new List<ParkOpeningHoursDateOverride>();
        JsonElement? overridesArray = GetArray(patch, "dateOverrides");
        if (overridesArray is null)
        {
            if (HasProperty(patch, "dateOverrides"))
            {
                errors.Add("openingHours.dateOverrides doit être un tableau.");
            }

            return overrides;
        }

        int index = 0;
        foreach (JsonElement overrideElement in overridesArray.Value.EnumerateArray())
        {
            string prefix = $"openingHours.dateOverrides[{index}]";
            if (overrideElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{prefix} doit être un objet.");
                index += 1;
                continue;
            }

            AddLegacyLocalizedOpeningHoursErrors(overrideElement, prefix, errors);

            ParkOpeningHoursDateOverride dateOverride = new ParkOpeningHoursDateOverride
            {
                LocalDate = ReadDateOnly(overrideElement, "localDate", $"{prefix}.localDate", errors),
                IsClosed = ReadBool(overrideElement, "isClosed") ?? false,
                Labels = ReadLocalizedTexts(GetArray(overrideElement, "labels")),
                Reasons = ReadLocalizedTexts(GetArray(overrideElement, "reasons")),
                TimeRanges = ReadOpeningHoursTimeRanges(overrideElement, $"{prefix}.timeRanges", errors),
            };

            overrides.Add(dateOverride);
            index += 1;
        }

        return overrides;
    }

    private static void AddLegacyLocalizedOpeningHoursErrors(JsonElement element, string prefix, List<string> errors)
    {
        if (HasProperty(element, "label"))
        {
            errors.Add($"{prefix}.label n'est plus accepté. Utilise labels avec des objets languageCode/value.");
        }

        if (HasProperty(element, "reason"))
        {
            errors.Add($"{prefix}.reason n'est plus accepté. Utilise reasons avec des objets languageCode/value.");
        }
    }

    private static List<ParkOpeningHoursTimeRange> ReadOpeningHoursTimeRanges(JsonElement patch, string fieldPrefix, List<string> errors)
    {
        List<ParkOpeningHoursTimeRange> timeRanges = new List<ParkOpeningHoursTimeRange>();
        JsonElement? rangesArray = GetArray(patch, "timeRanges");
        if (rangesArray is null)
        {
            if (HasProperty(patch, "timeRanges"))
            {
                errors.Add($"{fieldPrefix} doit être un tableau.");
            }

            return timeRanges;
        }

        int index = 0;
        foreach (JsonElement rangeElement in rangesArray.Value.EnumerateArray())
        {
            string prefix = $"{fieldPrefix}[{index}]";
            if (rangeElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{prefix} doit être un objet.");
                index += 1;
                continue;
            }

            ParkOpeningHoursTimeRange timeRange = new ParkOpeningHoursTimeRange
            {
                OpensAt = ReadTimeOnly(rangeElement, "opensAt", $"{prefix}.opensAt", errors),
                ClosesAt = ReadTimeOnly(rangeElement, "closesAt", $"{prefix}.closesAt", errors),
                ClosesNextDay = ReadBool(rangeElement, "closesNextDay") ?? false,
                LastAdmissionAt = ReadNullableTimeOnly(rangeElement, "lastAdmissionAt", $"{prefix}.lastAdmissionAt", errors),
                LastAdmissionNextDay = ReadBool(rangeElement, "lastAdmissionNextDay") ?? false,
            };

            timeRanges.Add(timeRange);
            index += 1;
        }

        return timeRanges;
    }

    private static List<DayOfWeek> ReadDaysOfWeek(JsonElement patch, string fieldName, List<string> errors)
    {
        List<DayOfWeek> daysOfWeek = new List<DayOfWeek>();
        JsonElement? daysArray = GetArray(patch, "daysOfWeek");
        if (daysArray is null)
        {
            if (HasProperty(patch, "daysOfWeek"))
            {
                errors.Add($"{fieldName} doit être un tableau.");
            }

            return daysOfWeek;
        }

        int index = 0;
        foreach (JsonElement dayElement in daysArray.Value.EnumerateArray())
        {
            string? value = dayElement.ValueKind == JsonValueKind.String ? dayElement.GetString() : dayElement.ToString();
            if (string.IsNullOrWhiteSpace(value) || !TryReadEnum(value, out DayOfWeek dayOfWeek))
            {
                errors.Add($"{fieldName}[{index}] est invalide.");
            }
            else
            {
                daysOfWeek.Add(dayOfWeek);
            }

            index += 1;
        }

        return daysOfWeek;
    }

    private static DateOnly ReadDateOnly(JsonElement patch, string propertyName, string fieldName, List<string> errors)
    {
        string? value = ReadString(patch, propertyName);
        if (!DateOnly.TryParseExact(value, OpeningHoursDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            errors.Add($"{fieldName} doit utiliser le format {OpeningHoursDateFormat}.");
            return default;
        }

        return date;
    }

    private static TimeOnly ReadTimeOnly(JsonElement patch, string propertyName, string fieldName, List<string> errors)
    {
        string? value = ReadString(patch, propertyName);
        if (!TimeOnly.TryParseExact(value, OpeningHoursTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly time))
        {
            errors.Add($"{fieldName} doit utiliser le format {OpeningHoursTimeFormat}.");
            return default;
        }

        return time;
    }

    private static TimeOnly? ReadNullableTimeOnly(JsonElement patch, string propertyName, string fieldName, List<string> errors)
    {
        if (!HasProperty(patch, propertyName) || HasNull(patch, propertyName))
        {
            return null;
        }

        return ReadTimeOnly(patch, propertyName, fieldName, errors);
    }

    private static void AddOpeningHoursValidationErrors(ParkGraphUpsertResult result, ApplicationResult<ParkOpeningHoursSchedule> normalizedResult)
    {
        foreach (ApplicationError error in normalizedResult.Errors)
        {
            if (error.Details is null || error.Details.Count == 0)
            {
                result.Errors.Add($"openingHours: {error.Message}");
                continue;
            }

            foreach (KeyValuePair<string, IReadOnlyCollection<string>> detail in error.Details)
            {
                result.Errors.Add($"openingHours.{detail.Key}: {string.Join(", ", detail.Value)}");
            }
        }
    }

    private static void AddOpeningHoursChanges(
        ParkGraphUpsertChange change,
        ParkOpeningHoursSchedule? existingSchedule,
        ParkOpeningHoursSchedule normalizedSchedule)
    {
        AddChange(change, "openingHours.timeZoneId", existingSchedule?.TimeZoneId, normalizedSchedule.TimeZoneId);
        AddChange(change, "openingHours.sourceUrl", existingSchedule?.SourceUrl, normalizedSchedule.SourceUrl);
        AddChange(change, "openingHours.notes", existingSchedule?.Notes, normalizedSchedule.Notes);
        AddChange(change, "openingHours.lastVerifiedAtUtc", existingSchedule?.LastVerifiedAtUtc, normalizedSchedule.LastVerifiedAtUtc);
        AddChange(change, "openingHours.regularRules", DescribeOpeningHoursRules(existingSchedule?.RegularRules), DescribeOpeningHoursRules(normalizedSchedule.RegularRules));
        AddChange(change, "openingHours.dateOverrides", DescribeOpeningHoursDateOverrides(existingSchedule?.DateOverrides), DescribeOpeningHoursDateOverrides(normalizedSchedule.DateOverrides));
    }

    private static bool HasOpeningHoursData(ParkOpeningHoursSchedule schedule)
    {
        return schedule.RegularRules.Count > 0 || schedule.DateOverrides.Count > 0;
    }

    private static string DescribeOpeningHoursRules(IReadOnlyCollection<ParkOpeningHoursRule>? rules)
    {
        if (rules is null || rules.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" || ", rules
            .OrderBy(static rule => rule.SortOrder)
            .ThenBy(static rule => rule.StartDate)
            .Select(static rule =>
            {
                string days = string.Join(",", rule.DaysOfWeek.OrderBy(static day => day));
                return string.Join("|", new[]
                {
                    rule.Id ?? string.Empty,
                    FormatOpeningHoursDate(rule.StartDate),
                    FormatOpeningHoursDate(rule.EndDate),
                    days,
                    FormatValue(rule.IsClosed) ?? string.Empty,
                    DescribeLocalizedTextsForDiff(rule.Labels),
                    DescribeLocalizedTextsForDiff(rule.Reasons),
                    FormatValue(rule.SortOrder) ?? string.Empty,
                    DescribeOpeningHoursTimeRanges(rule.TimeRanges),
                });
            }));
    }

    private static string DescribeOpeningHoursDateOverrides(IReadOnlyCollection<ParkOpeningHoursDateOverride>? dateOverrides)
    {
        if (dateOverrides is null || dateOverrides.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" || ", dateOverrides
            .OrderBy(static dateOverride => dateOverride.LocalDate)
            .Select(static dateOverride => string.Join("|", new[]
            {
                FormatOpeningHoursDate(dateOverride.LocalDate),
                FormatValue(dateOverride.IsClosed) ?? string.Empty,
                DescribeLocalizedTextsForDiff(dateOverride.Labels),
                DescribeLocalizedTextsForDiff(dateOverride.Reasons),
                DescribeOpeningHoursTimeRanges(dateOverride.TimeRanges),
            })));
    }

    private static string DescribeOpeningHoursTimeRanges(IReadOnlyCollection<ParkOpeningHoursTimeRange> timeRanges)
    {
        if (timeRanges.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(",", timeRanges
            .OrderBy(static timeRange => timeRange.OpensAt)
            .ThenBy(static timeRange => timeRange.ClosesNextDay)
            .ThenBy(static timeRange => timeRange.ClosesAt)
            .Select(static timeRange =>
            {
                string lastAdmission = timeRange.LastAdmissionAt.HasValue ? FormatOpeningHoursTime(timeRange.LastAdmissionAt.Value) : string.Empty;
                return string.Join("-", new[]
                {
                    FormatOpeningHoursTime(timeRange.OpensAt),
                    FormatOpeningHoursTime(timeRange.ClosesAt),
                    FormatValue(timeRange.ClosesNextDay) ?? string.Empty,
                    lastAdmission,
                    FormatValue(timeRange.LastAdmissionNextDay) ?? string.Empty,
                });
            }));
    }

    private static string FormatOpeningHoursDate(DateOnly date)
    {
        return date.ToString(OpeningHoursDateFormat, CultureInfo.InvariantCulture);
    }

    private static string FormatOpeningHoursTime(TimeOnly time)
    {
        return time.ToString(OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }
}
