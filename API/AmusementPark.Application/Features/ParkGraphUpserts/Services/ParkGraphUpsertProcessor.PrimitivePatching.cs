using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private static void PatchString(JsonElement? patch, string propertyName, string? current, Action<string?> assign, ParkGraphUpsertChange change, string? fieldName = null)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        string? next = ReadStringAllowNull(patch, propertyName)?.Trim();
        if (string.IsNullOrWhiteSpace(next))
        {
            next = null;
        }

        AddChange(change, fieldName ?? propertyName, current, next);
        assign(next);
    }
    private static void PatchBool(JsonElement? patch, string propertyName, bool current, Action<bool> assign, ParkGraphUpsertChange change)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        bool? next = ReadBool(patch, propertyName);
        if (!next.HasValue)
        {
            return;
        }

        AddChange(change, propertyName, current, next.Value);
        assign(next.Value);
    }
    private static void PatchBoolNullable(JsonElement? patch, string propertyName, bool? current, Action<bool?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        bool? next = ReadBool(patch, propertyName);
        AddChange(change, fieldName, current, next);
        assign(next);
    }
    private static void PatchInt(JsonElement? patch, string propertyName, int current, Action<int> assign, ParkGraphUpsertChange change)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        int? next = ReadInt(patch, propertyName);
        if (!next.HasValue)
        {
            return;
        }

        AddChange(change, propertyName, current, next.Value);
        assign(next.Value);
    }
    private static void PatchIntNullable(JsonElement? patch, string propertyName, int? current, Action<int?> assign, ParkGraphUpsertChange change, string? fieldName = null)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        int? next = ReadInt(patch, propertyName);
        AddChange(change, fieldName ?? propertyName, current, next);
        assign(next);
    }
    private static void PatchDoubleNullable(JsonElement? patch, string propertyName, double? current, Action<double?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        double? next = ReadDouble(patch, propertyName);
        AddChange(change, fieldName, current, next);
        assign(next);
    }
    private static void PatchDateNullable(JsonElement? patch, string propertyName, DateTime? current, Action<DateTime?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        DateTime? next = ReadDate(patch, propertyName);
        AddChange(change, fieldName, current?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), next?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        assign(next);
    }
    private static void PatchLifecycleDate(
        JsonElement? patch,
        string datePropertyName,
        string dateTextPropertyName,
        DateTime? currentDate,
        string? currentDateText,
        Action<DateTime?> assignDate,
        Action<string?> assignDateText,
        ParkGraphUpsertChange change,
        string dateFieldName,
        string dateTextFieldName)
    {
        bool hasDate = HasProperty(patch, datePropertyName);
        bool hasDateText = HasProperty(patch, dateTextPropertyName);
        if (hasDate)
        {
            string? rawDate = ReadStringAllowNull(patch, datePropertyName)?.Trim();
            if (string.IsNullOrWhiteSpace(rawDate))
            {
                PatchLifecycleDateValues(null, null, currentDate, currentDateText, assignDate, assignDateText, change, dateFieldName, dateTextFieldName, hasDateText);
            }
            else
            {
                DateTime? exactDate = ReadDate(patch, datePropertyName);
                if (exactDate.HasValue)
                {
                    string normalizedDateText = exactDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    PatchLifecycleDateValues(exactDate.Value, normalizedDateText, currentDate, currentDateText, assignDate, assignDateText, change, dateFieldName, dateTextFieldName, hasDateText);
                }
                else if (TryNormalizePartialLifecycleDateText(rawDate, out string? normalizedPartialDateText))
                {
                    PatchLifecycleDateValues(null, normalizedPartialDateText, currentDate, currentDateText, assignDate, assignDateText, change, dateFieldName, dateTextFieldName, hasDateText);
                }
                else
                {
                    PatchLifecycleDateValues(null, null, currentDate, currentDateText, assignDate, assignDateText, change, dateFieldName, dateTextFieldName, hasDateText);
                }
            }
        }

        if (hasDateText)
        {
            PatchString(patch, dateTextPropertyName, currentDateText, assignDateText, change, dateTextFieldName);
        }
    }

    private static void PatchLifecycleDateValues(
        DateTime? nextDate,
        string? inferredDateText,
        DateTime? currentDate,
        string? currentDateText,
        Action<DateTime?> assignDate,
        Action<string?> assignDateText,
        ParkGraphUpsertChange change,
        string dateFieldName,
        string dateTextFieldName,
        bool hasExplicitDateText)
    {
        AddChange(change, dateFieldName, currentDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), nextDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        assignDate(nextDate);

        if (!hasExplicitDateText)
        {
            AddChange(change, dateTextFieldName, currentDateText, inferredDateText);
            assignDateText(inferredDateText);
        }
    }

    private static bool TryNormalizePartialLifecycleDateText(string value, out string? normalized)
    {
        string trimmed = value.Trim();
        normalized = null;
        if (trimmed.Length == 4
            && trimmed.All(static character => char.IsDigit(character))
            && int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)
            && year is >= 1800 and <= 2100)
        {
            normalized = trimmed;
            return true;
        }

        string[] parts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2
            && parts[0].Length == 4
            && parts[0].All(static character => char.IsDigit(character))
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int monthYear)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int month)
            && monthYear is >= 1800 and <= 2100
            && month is >= 1 and <= 12)
        {
            normalized = string.Create(CultureInfo.InvariantCulture, $"{monthYear:0000}-{month:00}");
            return true;
        }

        return false;
    }
    private static void PatchEnum<T>(JsonElement? patch, string propertyName, T current, Action<T> assign, ParkGraphUpsertChange change)
        where T : struct, Enum
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        T? next = ReadEnumNullable<T>(patch, propertyName);
        if (!next.HasValue)
        {
            return;
        }

        AddChange(change, propertyName, current, next.Value);
        assign(next.Value);
    }
    private static void PatchEnumNullable<T>(JsonElement? patch, string propertyName, T? current, Action<T?> assign, ParkGraphUpsertChange change, string fieldName)
        where T : struct, Enum
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        T? next = ReadEnumNullable<T>(patch, propertyName);
        AddChange(change, fieldName, current, next);
        assign(next);
    }
    private static void PatchLocationPoint(JsonElement? patch, string propertyName, GeoPoint? current, Action<GeoPoint?> assign, ParkGraphUpsertChange change, string fieldName)
    {
        if (!HasProperty(patch, propertyName))
        {
            return;
        }

        JsonElement? point = GetObject(patch, propertyName);
        GeoPoint? next = null;
        if (point is not null)
        {
            double? latitude = ReadDouble(point, "latitude");
            double? longitude = ReadDouble(point, "longitude");
            if (latitude.HasValue && longitude.HasValue)
            {
                next = new GeoPoint(latitude.Value, longitude.Value);
            }
        }

        AddChange(change, fieldName, FormatPosition(current), FormatPosition(next));
        assign(next);
    }
}
