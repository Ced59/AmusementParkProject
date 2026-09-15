using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Core.Geo;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Parks.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System.Text;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;
internal static class ParkGraphUpsertProcessorOpeningHoursExtensions
{
    internal static async Task ProcessOpeningHoursAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, Park targetPark, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        await processorContext.ProcessPricingAsync(root, targetPark, result, apply, cancellationToken);
        if (!ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursPatch(root))
        {
            return;
        }

        JsonElement? patch = ParkGraphUpsertProcessorOpeningHoursExtensions.ResolveOpeningHoursPatch(root);
        ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange(ParkGraphUpsertProcessor.OpeningHoursEntityType, targetPark.Id, ParkGraphUpsertProcessor.OpeningHoursPropertyName, string.IsNullOrWhiteSpace(targetPark.Name) ? targetPark.Id : $"{targetPark.Name} opening hours", "Unchanged", ParkGraphUpsertProcessor.OpeningHoursPropertyName);
        if (patch is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("openingHours doit être un objet JSON.");
            return;
        }

        if (!ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursScheduleData(patch.Value))
        {
            return;
        }

        if (!targetPark.Status.CanHaveCurrentOpeningHours())
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add($"openingHours est réservé aux parcs dont le statut est '{ParkStatus.Operating}'. Le parc cible utilise '{targetPark.Status}'.");
            return;
        }

        if (processorContext.parkOpeningHoursRepository is null
            || processorContext.parkOpeningHoursScheduleNormalizer is null
            || processorContext.parkOpeningHoursCoverageSegmentBuilder is null
            || (apply && processorContext.openingHoursFactualChangeCapture is null))
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            result.Errors.Add("Le traitement des horaires n'est pas disponible dans ce contexte.");
            return;
        }

        List<string> readErrors = new List<string>();
        ParkOpeningHoursSchedule schedule = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadOpeningHoursSchedule(patch.Value, targetPark.Id, readErrors);
        string? requestedParkId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "parkId");
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

        ApplicationResult<ParkOpeningHoursSchedule> normalizedResult = processorContext.parkOpeningHoursScheduleNormalizer.Normalize(schedule);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            ParkGraphUpsertProcessorOpeningHoursExtensions.AddOpeningHoursValidationErrors(result, normalizedResult);
            return;
        }

        ParkOpeningHoursSchedule normalizedSchedule = normalizedResult.Value;
        ParkOpeningHoursSchedule? existingSchedule = await processorContext.parkOpeningHoursRepository.GetByParkIdAsync(targetPark.Id, cancellationToken);
        bool isNew = existingSchedule is null || !ParkGraphUpsertProcessorOpeningHoursExtensions.HasOpeningHoursData(existingSchedule);
        ParkGraphUpsertProcessorOpeningHoursExtensions.AddOpeningHoursChanges(change, existingSchedule, normalizedSchedule);
        if (change.Fields.Count > 0 || isNew)
        {
            change.ChangeType = isNew ? "Created" : "Updated";
        }

        result.Changes.Add(change);
        if (!apply)
        {
            return;
        }

        normalizedSchedule.CoverageSegments = processorContext.parkOpeningHoursCoverageSegmentBuilder.BuildSegments(normalizedSchedule).ToList();
        ParkOpeningHoursSchedule savedSchedule =
            await processorContext.parkOpeningHoursRepository.UpsertAsync(
                normalizedSchedule,
                cancellationToken);
        IParkOpeningHoursFactualChangeCapture factualChangeCapture =
            processorContext.openingHoursFactualChangeCapture
            ?? throw new InvalidOperationException(
                "The opening-hours factual change capture is unavailable after commit.");
        await factualChangeCapture.CaptureAsync(
            targetPark,
            existingSchedule,
            savedSchedule,
            CancellationToken.None);
    }

    internal static bool HasOpeningHoursPatch(JsonElement root)
    {
        return ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(root, ParkGraphUpsertProcessor.OpeningHoursPropertyName) || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(root, ParkGraphUpsertProcessor.LegacyOpeningHoursPropertyName);
    }

    internal static JsonElement? ResolveOpeningHoursPatch(JsonElement? root)
    {
        return ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, ParkGraphUpsertProcessor.OpeningHoursPropertyName) ?? ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, ParkGraphUpsertProcessor.LegacyOpeningHoursPropertyName);
    }

    internal static bool HasOpeningHoursScheduleData(JsonElement patch)
    {
        return ParkGraphUpsertProcessorOpeningHoursExtensions.HasNonEmptyArrayValue(patch, "regularRules") || ParkGraphUpsertProcessorOpeningHoursExtensions.HasNonEmptyArrayValue(patch, "dateOverrides");
    }

    internal static bool HasNonEmptyArrayValue(JsonElement patch, string propertyName)
    {
        if (!patch.TryGetProperty(propertyName, out JsonElement value))
        {
            return false;
        }

        return value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 0;
    }

    internal static ParkOpeningHoursSchedule ReadOpeningHoursSchedule(JsonElement patch, string targetParkId, List<string> errors)
    {
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = targetParkId,
            TimeZoneId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "timeZoneId") ?? string.Empty,
            SourceUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "sourceUrl"),
            Notes = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "notes"),
            LastVerifiedAtUtc = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDate(patch, "lastVerifiedAtUtc"),
            RegularRules = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadOpeningHoursRules(patch, errors),
            DateOverrides = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadOpeningHoursDateOverrides(patch, errors),
        };
        return schedule;
    }

    internal static List<ParkOpeningHoursRule> ReadOpeningHoursRules(JsonElement patch, List<string> errors)
    {
        List<ParkOpeningHoursRule> rules = new List<ParkOpeningHoursRule>();
        JsonElement? rulesArray = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "regularRules");
        if (rulesArray is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "regularRules"))
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

            ParkGraphUpsertProcessorOpeningHoursExtensions.AddLegacyLocalizedOpeningHoursErrors(ruleElement, prefix, errors);
            ParkOpeningHoursRule rule = new ParkOpeningHoursRule
            {
                Id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(ruleElement, "id"),
                StartDate = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadDateOnly(ruleElement, "startDate", $"{prefix}.startDate", errors),
                EndDate = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadDateOnly(ruleElement, "endDate", $"{prefix}.endDate", errors),
                DaysOfWeek = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadDaysOfWeek(ruleElement, $"{prefix}.daysOfWeek", errors),
                IsClosed = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(ruleElement, "isClosed") ?? false,
                Labels = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(ruleElement, "labels")),
                Reasons = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(ruleElement, "reasons")),
                SortOrder = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(ruleElement, "sortOrder") ?? index + 1,
                TimeRanges = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadOpeningHoursTimeRanges(ruleElement, $"{prefix}.timeRanges", errors),
            };
            rules.Add(rule);
            index += 1;
        }

        return rules;
    }

    internal static List<ParkOpeningHoursDateOverride> ReadOpeningHoursDateOverrides(JsonElement patch, List<string> errors)
    {
        List<ParkOpeningHoursDateOverride> overrides = new List<ParkOpeningHoursDateOverride>();
        JsonElement? overridesArray = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "dateOverrides");
        if (overridesArray is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "dateOverrides"))
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

            ParkGraphUpsertProcessorOpeningHoursExtensions.AddLegacyLocalizedOpeningHoursErrors(overrideElement, prefix, errors);
            ParkOpeningHoursDateOverride dateOverride = new ParkOpeningHoursDateOverride
            {
                LocalDate = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadDateOnly(overrideElement, "localDate", $"{prefix}.localDate", errors),
                IsClosed = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(overrideElement, "isClosed") ?? false,
                Labels = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(overrideElement, "labels")),
                Reasons = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(overrideElement, "reasons")),
                TimeRanges = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadOpeningHoursTimeRanges(overrideElement, $"{prefix}.timeRanges", errors),
            };
            overrides.Add(dateOverride);
            index += 1;
        }

        return overrides;
    }

    internal static void AddLegacyLocalizedOpeningHoursErrors(JsonElement element, string prefix, List<string> errors)
    {
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, "label"))
        {
            errors.Add($"{prefix}.label n'est plus accepté. Utilise labels avec des objets languageCode/value.");
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, "reason"))
        {
            errors.Add($"{prefix}.reason n'est plus accepté. Utilise reasons avec des objets languageCode/value.");
        }
    }

    internal static List<ParkOpeningHoursTimeRange> ReadOpeningHoursTimeRanges(JsonElement patch, string fieldPrefix, List<string> errors)
    {
        List<ParkOpeningHoursTimeRange> timeRanges = new List<ParkOpeningHoursTimeRange>();
        JsonElement? rangesArray = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "timeRanges");
        if (rangesArray is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "timeRanges"))
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
                OpensAt = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadTimeOnly(rangeElement, "opensAt", $"{prefix}.opensAt", errors),
                ClosesAt = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadTimeOnly(rangeElement, "closesAt", $"{prefix}.closesAt", errors),
                ClosesNextDay = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(rangeElement, "closesNextDay") ?? false,
                LastAdmissionAt = ParkGraphUpsertProcessorOpeningHoursExtensions.ReadNullableTimeOnly(rangeElement, "lastAdmissionAt", $"{prefix}.lastAdmissionAt", errors),
                LastAdmissionNextDay = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(rangeElement, "lastAdmissionNextDay") ?? false,
            };
            timeRanges.Add(timeRange);
            index += 1;
        }

        return timeRanges;
    }

    internal static List<DayOfWeek> ReadDaysOfWeek(JsonElement patch, string fieldName, List<string> errors)
    {
        List<DayOfWeek> daysOfWeek = new List<DayOfWeek>();
        JsonElement? daysArray = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "daysOfWeek");
        if (daysArray is null)
        {
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "daysOfWeek"))
            {
                errors.Add($"{fieldName} doit être un tableau.");
            }

            return daysOfWeek;
        }

        int index = 0;
        foreach (JsonElement dayElement in daysArray.Value.EnumerateArray())
        {
            string? value = dayElement.ValueKind == JsonValueKind.String ? dayElement.GetString() : dayElement.ToString();
            if (string.IsNullOrWhiteSpace(value) || !ParkGraphUpsertProcessorJsonReadingExtensions.TryReadEnum(value, out DayOfWeek dayOfWeek))
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

    internal static DateOnly ReadDateOnly(JsonElement patch, string propertyName, string fieldName, List<string> errors)
    {
        string? value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, propertyName);
        if (!DateOnly.TryParseExact(value, ParkGraphUpsertProcessor.OpeningHoursDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            errors.Add($"{fieldName} doit utiliser le format {ParkGraphUpsertProcessor.OpeningHoursDateFormat}.");
            return default;
        }

        return date;
    }

    internal static TimeOnly ReadTimeOnly(JsonElement patch, string propertyName, string fieldName, List<string> errors)
    {
        string? value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, propertyName);
        if (!TimeOnly.TryParseExact(value, ParkGraphUpsertProcessor.OpeningHoursTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly time))
        {
            errors.Add($"{fieldName} doit utiliser le format {ParkGraphUpsertProcessor.OpeningHoursTimeFormat}.");
            return default;
        }

        return time;
    }

    internal static TimeOnly? ReadNullableTimeOnly(JsonElement patch, string propertyName, string fieldName, List<string> errors)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName) || ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(patch, propertyName))
        {
            return null;
        }

        return ParkGraphUpsertProcessorOpeningHoursExtensions.ReadTimeOnly(patch, propertyName, fieldName, errors);
    }

    internal static void AddOpeningHoursValidationErrors(ParkGraphUpsertResult result, ApplicationResult<ParkOpeningHoursSchedule> normalizedResult)
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

    internal static void AddOpeningHoursChanges(ParkGraphUpsertChange change, ParkOpeningHoursSchedule? existingSchedule, ParkOpeningHoursSchedule normalizedSchedule)
    {
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "openingHours.timeZoneId", existingSchedule?.TimeZoneId, normalizedSchedule.TimeZoneId);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "openingHours.sourceUrl", existingSchedule?.SourceUrl, normalizedSchedule.SourceUrl);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "openingHours.notes", existingSchedule?.Notes, normalizedSchedule.Notes);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "openingHours.lastVerifiedAtUtc", existingSchedule?.LastVerifiedAtUtc, normalizedSchedule.LastVerifiedAtUtc);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "openingHours.regularRules", ParkGraphUpsertProcessorOpeningHoursExtensions.DescribeOpeningHoursRules(existingSchedule?.RegularRules), ParkGraphUpsertProcessorOpeningHoursExtensions.DescribeOpeningHoursRules(normalizedSchedule.RegularRules));
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "openingHours.dateOverrides", ParkGraphUpsertProcessorOpeningHoursExtensions.DescribeOpeningHoursDateOverrides(existingSchedule?.DateOverrides), ParkGraphUpsertProcessorOpeningHoursExtensions.DescribeOpeningHoursDateOverrides(normalizedSchedule.DateOverrides));
    }

    internal static bool HasOpeningHoursData(ParkOpeningHoursSchedule schedule)
    {
        return schedule.RegularRules.Count > 0 || schedule.DateOverrides.Count > 0;
    }

    internal static string DescribeOpeningHoursRules(IReadOnlyCollection<ParkOpeningHoursRule>? rules)
    {
        if (rules is null || rules.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" || ", rules.OrderBy(static rule => rule.SortOrder).ThenBy(static rule => rule.StartDate).Select(static rule =>
        {
            string days = string.Join(",", rule.DaysOfWeek.OrderBy(static day => day));
            return string.Join("|", new[] { rule.Id ?? string.Empty, ParkGraphUpsertProcessorOpeningHoursExtensions.FormatOpeningHoursDate(rule.StartDate), ParkGraphUpsertProcessorOpeningHoursExtensions.FormatOpeningHoursDate(rule.EndDate), days, ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(rule.IsClosed) ?? string.Empty, ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(rule.Labels), ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(rule.Reasons), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(rule.SortOrder) ?? string.Empty, ParkGraphUpsertProcessorOpeningHoursExtensions.DescribeOpeningHoursTimeRanges(rule.TimeRanges), });
        }));
    }

    internal static string DescribeOpeningHoursDateOverrides(IReadOnlyCollection<ParkOpeningHoursDateOverride>? dateOverrides)
    {
        if (dateOverrides is null || dateOverrides.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" || ", dateOverrides.OrderBy(static dateOverride => dateOverride.LocalDate).Select(static dateOverride => string.Join("|", new[] { ParkGraphUpsertProcessorOpeningHoursExtensions.FormatOpeningHoursDate(dateOverride.LocalDate), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(dateOverride.IsClosed) ?? string.Empty, ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(dateOverride.Labels), ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(dateOverride.Reasons), ParkGraphUpsertProcessorOpeningHoursExtensions.DescribeOpeningHoursTimeRanges(dateOverride.TimeRanges), })));
    }

    internal static string DescribeOpeningHoursTimeRanges(IReadOnlyCollection<ParkOpeningHoursTimeRange> timeRanges)
    {
        if (timeRanges.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(",", timeRanges.OrderBy(static timeRange => timeRange.OpensAt).ThenBy(static timeRange => timeRange.ClosesNextDay).ThenBy(static timeRange => timeRange.ClosesAt).Select(static timeRange =>
        {
            string lastAdmission = timeRange.LastAdmissionAt.HasValue ? ParkGraphUpsertProcessorOpeningHoursExtensions.FormatOpeningHoursTime(timeRange.LastAdmissionAt.Value) : string.Empty;
            return string.Join("-", new[] { ParkGraphUpsertProcessorOpeningHoursExtensions.FormatOpeningHoursTime(timeRange.OpensAt), ParkGraphUpsertProcessorOpeningHoursExtensions.FormatOpeningHoursTime(timeRange.ClosesAt), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(timeRange.ClosesNextDay) ?? string.Empty, lastAdmission, ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(timeRange.LastAdmissionNextDay) ?? string.Empty, });
        }));
    }

    internal static string FormatOpeningHoursDate(DateOnly date)
    {
        return date.ToString(ParkGraphUpsertProcessor.OpeningHoursDateFormat, CultureInfo.InvariantCulture);
    }

    internal static string FormatOpeningHoursTime(TimeOnly time)
    {
        return time.ToString(ParkGraphUpsertProcessor.OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }
}
