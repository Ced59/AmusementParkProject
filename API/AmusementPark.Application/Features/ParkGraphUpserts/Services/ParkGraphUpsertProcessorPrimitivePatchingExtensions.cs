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
internal static class ParkGraphUpsertProcessorPrimitivePatchingExtensions
{
    internal static void PatchString(JsonElement? patch, string propertyName, string? current, Action<string?> assign, ParkGraphUpsertChange change, string? fieldName = null)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        string? next = ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(patch, propertyName)?.Trim();
        if (string.IsNullOrWhiteSpace(next))
        {
            next = null;
        }

        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, fieldName ?? propertyName, current, next);
        assign(next);
    }

    internal static void PatchBool(JsonElement? patch, string propertyName, bool current, Action<bool> assign, ParkGraphUpsertChange change)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        bool? next = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(patch, propertyName);
        if (!next.HasValue)
        {
            return;
        }

        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, propertyName, current, next.Value);
        assign(next.Value);
    }

    internal static void PatchBoolNullable(JsonElement? patch, string propertyName, bool? current, Action<bool?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        bool? next = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(patch, propertyName);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, fieldName, current, next);
        assign(next);
    }

    internal static void PatchInt(JsonElement? patch, string propertyName, int current, Action<int> assign, ParkGraphUpsertChange change)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        int? next = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(patch, propertyName);
        if (!next.HasValue)
        {
            return;
        }

        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, propertyName, current, next.Value);
        assign(next.Value);
    }

    internal static void PatchIntNullable(JsonElement? patch, string propertyName, int? current, Action<int?> assign, ParkGraphUpsertChange change, string? fieldName = null)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        int? next = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(patch, propertyName);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, fieldName ?? propertyName, current, next);
        assign(next);
    }

    internal static void PatchDoubleNullable(JsonElement? patch, string propertyName, double? current, Action<double?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        double? next = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(patch, propertyName);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, fieldName, current, next);
        assign(next);
    }

    internal static void PatchDateNullable(JsonElement? patch, string propertyName, DateTime? current, Action<DateTime?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        DateTime? next = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDate(patch, propertyName);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, fieldName, current?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), next?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        assign(next);
    }

    internal static void PatchLifecycleDate(JsonElement? patch, string datePropertyName, string dateTextPropertyName, DateTime? currentDate, string? currentDateText, Action<DateTime?> assignDate, Action<string?> assignDateText, ParkGraphUpsertChange change, string dateFieldName, string dateTextFieldName)
    {
        bool hasDate = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, datePropertyName);
        bool hasDateText = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, dateTextPropertyName);
        if (hasDate)
        {
            string? rawDate = ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(patch, datePropertyName)?.Trim();
            if (string.IsNullOrWhiteSpace(rawDate))
            {
                ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLifecycleDateValues(null, null, currentDate, currentDateText, assignDate, assignDateText, change, dateFieldName, dateTextFieldName, hasDateText);
            }
            else
            {
                DateTime? exactDate = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDate(patch, datePropertyName);
                if (exactDate.HasValue)
                {
                    string normalizedDateText = exactDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLifecycleDateValues(exactDate.Value, normalizedDateText, currentDate, currentDateText, assignDate, assignDateText, change, dateFieldName, dateTextFieldName, hasDateText);
                }
                else if (ParkGraphUpsertProcessorPrimitivePatchingExtensions.TryNormalizePartialLifecycleDateText(rawDate, out string? normalizedPartialDateText))
                {
                    ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLifecycleDateValues(null, normalizedPartialDateText, currentDate, currentDateText, assignDate, assignDateText, change, dateFieldName, dateTextFieldName, hasDateText);
                }
                else
                {
                    ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLifecycleDateValues(null, null, currentDate, currentDateText, assignDate, assignDateText, change, dateFieldName, dateTextFieldName, hasDateText);
                }
            }
        }

        if (hasDateText)
        {
            ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, dateTextPropertyName, currentDateText, assignDateText, change, dateTextFieldName);
        }
    }

    internal static void PatchLifecycleDateValues(DateTime? nextDate, string? inferredDateText, DateTime? currentDate, string? currentDateText, Action<DateTime?> assignDate, Action<string?> assignDateText, ParkGraphUpsertChange change, string dateFieldName, string dateTextFieldName, bool hasExplicitDateText)
    {
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, dateFieldName, currentDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), nextDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        assignDate(nextDate);
        if (!hasExplicitDateText)
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, dateTextFieldName, currentDateText, inferredDateText);
            assignDateText(inferredDateText);
        }
    }

    internal static bool TryNormalizePartialLifecycleDateText(string value, out string? normalized)
    {
        string trimmed = value.Trim();
        normalized = null;
        if (trimmed.Length == 4 && trimmed.All(static character => char.IsDigit(character)) && int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year) && year is >= 1800 and <= 2100)
        {
            normalized = trimmed;
            return true;
        }

        string[] parts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && parts[0].Length == 4 && parts[0].All(static character => char.IsDigit(character)) && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int monthYear) && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int month) && monthYear is >= 1800 and <= 2100 && month is >= 1 and <= 12)
        {
            normalized = string.Create(CultureInfo.InvariantCulture, $"{monthYear:0000}-{month:00}");
            return true;
        }

        return false;
    }

    internal static void PatchEnum<T>(JsonElement? patch, string propertyName, T current, Action<T> assign, ParkGraphUpsertChange change)
        where T : struct, Enum
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        T? next = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnumNullable<T>(patch, propertyName);
        if (!next.HasValue)
        {
            return;
        }

        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, propertyName, current, next.Value);
        assign(next.Value);
    }

    internal static void PatchEnumNullable<T>(JsonElement? patch, string propertyName, T? current, Action<T?> assign, ParkGraphUpsertChange change, string fieldName)
        where T : struct, Enum
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        T? next = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnumNullable<T>(patch, propertyName);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, fieldName, current, next);
        assign(next);
    }

    internal static void PatchLocationPoint(JsonElement? patch, string propertyName, GeoPoint? current, Action<GeoPoint?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, propertyName))
        {
            return;
        }

        JsonElement? point = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, propertyName);
        GeoPoint? next = null;
        if (point is not null)
        {
            double? latitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(point, "latitude");
            double? longitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(point, "longitude");
            if (latitude.HasValue && longitude.HasValue)
            {
                next = new GeoPoint(latitude.Value, longitude.Value);
            }
        }

        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, fieldName, ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(current), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(next));
        assign(next);
    }
}
