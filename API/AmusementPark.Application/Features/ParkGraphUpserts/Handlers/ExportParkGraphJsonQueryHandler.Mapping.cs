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

namespace AmusementPark.Application.Features.ParkGraphUpserts.Handlers;

public sealed partial class ExportParkGraphJsonQueryHandler
{
    private static ParkGraphExportOpeningHours MapOpeningHours(ParkOpeningHoursSchedule schedule)
    {
        return new ParkGraphExportOpeningHours
        {
            ParkId = schedule.ParkId,
            TimeZoneId = schedule.TimeZoneId,
            SourceUrl = schedule.SourceUrl,
            Notes = schedule.Notes,
            LastVerifiedAtUtc = schedule.LastVerifiedAtUtc,
            RegularRules = schedule.RegularRules
                .OrderBy(static rule => rule.SortOrder)
                .ThenBy(static rule => rule.StartDate)
                .Select(static rule => new ParkGraphExportOpeningHoursRule
                {
                    Id = rule.Id,
                    StartDate = FormatOpeningHoursDate(rule.StartDate),
                    EndDate = FormatOpeningHoursDate(rule.EndDate),
                    DaysOfWeek = rule.DaysOfWeek.Select(static day => day.ToString()).ToList(),
                    IsClosed = rule.IsClosed,
                    Labels = CopyLocalizedTexts(rule.Labels),
                    Reasons = CopyLocalizedTexts(rule.Reasons),
                    SortOrder = rule.SortOrder,
                    TimeRanges = rule.TimeRanges.Select(static timeRange => MapOpeningHoursTimeRange(timeRange)).ToList(),
                })
                .ToList(),
            DateOverrides = schedule.DateOverrides
                .OrderBy(static dateOverride => dateOverride.LocalDate)
                .Select(static dateOverride => new ParkGraphExportOpeningHoursDateOverride
                {
                    LocalDate = FormatOpeningHoursDate(dateOverride.LocalDate),
                    IsClosed = dateOverride.IsClosed,
                    Labels = CopyLocalizedTexts(dateOverride.Labels),
                    Reasons = CopyLocalizedTexts(dateOverride.Reasons),
                    TimeRanges = dateOverride.TimeRanges.Select(static timeRange => MapOpeningHoursTimeRange(timeRange)).ToList(),
                })
                .ToList(),
        };
    }

    private static ParkGraphExportOpeningHoursTimeRange MapOpeningHoursTimeRange(ParkOpeningHoursTimeRange timeRange)
    {
        return new ParkGraphExportOpeningHoursTimeRange
        {
            OpensAt = FormatOpeningHoursTime(timeRange.OpensAt),
            ClosesAt = FormatOpeningHoursTime(timeRange.ClosesAt),
            ClosesNextDay = timeRange.ClosesNextDay,
            LastAdmissionAt = timeRange.LastAdmissionAt.HasValue ? FormatOpeningHoursTime(timeRange.LastAdmissionAt.Value) : null,
            LastAdmissionNextDay = timeRange.LastAdmissionNextDay,
        };
    }

    private static string? BuildImageOwnerKey(Image image, string parkId)
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

    private static string BuildInternalImageUrl(string imageId)
    {
        return $"/images/{imageId}";
    }

    private static List<string> BuildDistinctIds(IEnumerable<string?> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToList();
    }

    private static ParkGraphExportFounder MapFounder(ParkFounder founder)
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
            Biography = CopyLocalizedTexts(founder.Biography),
        };
    }

    private static ParkGraphExportOperator MapOperator(ParkOperator parkOperator)
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
            Description = CopyLocalizedTexts(parkOperator.Description),
            AdminReviewStatus = parkOperator.AdminReviewStatus,
        };
    }

    private static ParkGraphExportManufacturer MapManufacturer(AttractionManufacturer manufacturer)
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
            Biography = CopyLocalizedTexts(manufacturer.Biography),
            IsVisible = manufacturer.IsVisible,
            AdminReviewStatus = manufacturer.AdminReviewStatus,
        };
    }

    private static List<LocalizedText> CopyLocalizedTexts(IReadOnlyCollection<LocalizedText> values)
    {
        return values
            .Select(static value => new LocalizedText(value.LanguageCode, value.Value))
            .ToList();
    }

    private static string BuildFileName(Park park, DateTime exportedAtUtc)
    {
        string sourceName = string.IsNullOrWhiteSpace(park.Name) ? park.Id : park.Name;
        string safeName = SanitizeFileName(sourceName);
        return $"{safeName}-{exportedAtUtc.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-park-graph.json";
    }

    private static string SanitizeFileName(string value)
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

    private static string FormatOpeningHoursDate(DateOnly date)
    {
        return date.ToString(OpeningHoursDateFormat, CultureInfo.InvariantCulture);
    }

    private static string FormatOpeningHoursTime(TimeOnly time)
    {
        return time.ToString(OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }
}
