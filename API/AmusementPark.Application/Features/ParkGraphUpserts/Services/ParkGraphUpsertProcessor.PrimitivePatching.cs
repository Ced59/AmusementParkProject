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
