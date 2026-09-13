using System.Globalization;
using System.Text;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Application.Common.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
internal static class ExportParkGraphJsonQueryHandlerMappingExtensions
{
    internal static ParkGraphExportOpeningHours MapOpeningHours(ParkOpeningHoursSchedule schedule)
    {
        return new ParkGraphExportOpeningHours
        {
            ParkId = schedule.ParkId,
            TimeZoneId = schedule.TimeZoneId,
            SourceUrl = schedule.SourceUrl,
            Notes = schedule.Notes,
            LastVerifiedAtUtc = schedule.LastVerifiedAtUtc,
            RegularRules = schedule.RegularRules.OrderBy(static rule => rule.SortOrder).ThenBy(static rule => rule.StartDate).Select(static rule => new ParkGraphExportOpeningHoursRule { Id = rule.Id, StartDate = ExportParkGraphJsonQueryHandlerMappingExtensions.FormatOpeningHoursDate(rule.StartDate), EndDate = ExportParkGraphJsonQueryHandlerMappingExtensions.FormatOpeningHoursDate(rule.EndDate), DaysOfWeek = rule.DaysOfWeek.Select(static day => day.ToString()).ToList(), IsClosed = rule.IsClosed, Labels = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(rule.Labels), Reasons = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(rule.Reasons), SortOrder = rule.SortOrder, TimeRanges = rule.TimeRanges.Select(static timeRange => ExportParkGraphJsonQueryHandlerMappingExtensions.MapOpeningHoursTimeRange(timeRange)).ToList(), }).ToList(),
            DateOverrides = schedule.DateOverrides.OrderBy(static dateOverride => dateOverride.LocalDate).Select(static dateOverride => new ParkGraphExportOpeningHoursDateOverride { LocalDate = ExportParkGraphJsonQueryHandlerMappingExtensions.FormatOpeningHoursDate(dateOverride.LocalDate), IsClosed = dateOverride.IsClosed, Labels = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(dateOverride.Labels), Reasons = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(dateOverride.Reasons), TimeRanges = dateOverride.TimeRanges.Select(static timeRange => ExportParkGraphJsonQueryHandlerMappingExtensions.MapOpeningHoursTimeRange(timeRange)).ToList(), }).ToList(),
        };
    }

    internal static ParkGraphExportOpeningHoursTimeRange MapOpeningHoursTimeRange(ParkOpeningHoursTimeRange timeRange)
    {
        return new ParkGraphExportOpeningHoursTimeRange
        {
            OpensAt = ExportParkGraphJsonQueryHandlerMappingExtensions.FormatOpeningHoursTime(timeRange.OpensAt),
            ClosesAt = ExportParkGraphJsonQueryHandlerMappingExtensions.FormatOpeningHoursTime(timeRange.ClosesAt),
            ClosesNextDay = timeRange.ClosesNextDay,
            LastAdmissionAt = timeRange.LastAdmissionAt.HasValue ? ExportParkGraphJsonQueryHandlerMappingExtensions.FormatOpeningHoursTime(timeRange.LastAdmissionAt.Value) : null,
            LastAdmissionNextDay = timeRange.LastAdmissionNextDay,
        };
    }

    internal static string? BuildImageOwnerKey(Image image, string parkId)
    {
        if (image.OwnerType == ImageOwnerType.Park && string.Equals(image.OwnerId, parkId, StringComparison.Ordinal))
        {
            return "park";
        }

        if (image.OwnerType == ImageOwnerType.ParkItem)
        {
            return image.OwnerId;
        }

        if (image.OwnerType == ImageOwnerType.ParkOperator)
        {
            return string.IsNullOrWhiteSpace(image.OwnerId) ? null : $"operator:{image.OwnerId}";
        }

        if (image.OwnerType == ImageOwnerType.ParkFounder)
        {
            return string.IsNullOrWhiteSpace(image.OwnerId) ? null : $"founder:{image.OwnerId}";
        }

        if (image.OwnerType == ImageOwnerType.AttractionManufacturer)
        {
            return string.IsNullOrWhiteSpace(image.OwnerId) ? null : $"manufacturer:{image.OwnerId}";
        }

        return image.OwnerId;
    }

    internal static string BuildInternalImageUrl(string imageId)
    {
        return $"/images/{imageId}";
    }

    internal static List<string> BuildDistinctIds(IEnumerable<string?> values)
    {
        return values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value ?? string.Empty).Distinct(StringComparer.Ordinal).OrderBy(static value => value, StringComparer.Ordinal).ToList();
    }

    internal static ParkGraphExportFounder MapFounder(ParkFounder founder)
    {
        return new ParkGraphExportFounder
        {
            Key = founder.Id,
            Id = founder.Id,
            Name = founder.Name,
            Occupation = founder.Occupation,
            BirthDate = founder.BirthDate,
            DeathDate = founder.DeathDate,
            BirthPlace = founder.BirthPlace,
            NationalityCountryCode = founder.NationalityCountryCode,
            WebsiteUrl = founder.WebsiteUrl,
            Biography = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(founder.Biography),
        };
    }

    internal static ParkGraphExportOperator MapOperator(ParkOperator parkOperator)
    {
        return new ParkGraphExportOperator
        {
            Key = parkOperator.Id,
            Id = parkOperator.Id,
            Name = parkOperator.Name,
            LegalName = parkOperator.LegalName,
            FoundedYear = parkOperator.FoundedYear,
            ClosedYear = parkOperator.ClosedYear,
            ContactDetails = parkOperator.ContactDetails,
            Description = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(parkOperator.Description),
            AdminReviewStatus = parkOperator.AdminReviewStatus,
        };
    }

    internal static ParkGraphExportManufacturer MapManufacturer(AttractionManufacturer manufacturer)
    {
        return new ParkGraphExportManufacturer
        {
            Key = manufacturer.Id,
            Id = manufacturer.Id,
            Name = manufacturer.Name,
            LegalName = manufacturer.LegalName,
            FoundedYear = manufacturer.FoundedYear,
            ClosedYear = manufacturer.ClosedYear,
            ContactDetails = manufacturer.ContactDetails,
            Biography = ExportParkGraphJsonQueryHandlerMappingExtensions.CopyLocalizedTexts(manufacturer.Biography),
            IsVisible = manufacturer.IsVisible,
            AdminReviewStatus = manufacturer.AdminReviewStatus,
        };
    }

    internal static List<LocalizedText> CopyLocalizedTexts(IReadOnlyCollection<LocalizedText> values)
    {
        return values.Select(static value => new LocalizedText(value.LanguageCode, value.Value)).ToList();
    }

    internal static string BuildFileName(Park park, DateTime exportedAtUtc)
    {
        string sourceName = string.IsNullOrWhiteSpace(park.Name) ? park.Id : park.Name;
        string safeName = ExportParkGraphJsonQueryHandlerMappingExtensions.SanitizeFileName(sourceName);
        return $"{safeName}-{exportedAtUtc.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-park-graph.json";
    }

    internal static string SanitizeFileName(string value)
    {
        StringBuilder builder = new StringBuilder();
        foreach (char character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character == '-' || character == '_')
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                builder.Append('-');
            }
        }

        string result = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "park" : result;
    }

    internal static string FormatOpeningHoursDate(DateOnly date)
    {
        return date.ToString(ExportParkGraphJsonQueryHandler.OpeningHoursDateFormat, CultureInfo.InvariantCulture);
    }

    internal static string FormatOpeningHoursTime(TimeOnly time)
    {
        return time.ToString(ExportParkGraphJsonQueryHandler.OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }
}
