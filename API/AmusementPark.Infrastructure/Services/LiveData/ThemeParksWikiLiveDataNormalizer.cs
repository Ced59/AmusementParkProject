using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Infrastructure.Services.LiveData;

internal static class ThemeParksWikiLiveDataNormalizer
{
    public static IReadOnlyCollection<ExternalLiveObservation> Normalize(
        ThemeParksWikiLiveDataResponse response,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(diagnostics);

        List<ExternalLiveObservation> observations = new List<ExternalLiveObservation>();
        if (response.LiveData is null || response.LiveData.Count == 0)
        {
            diagnostics.Add(new LiveProviderDiagnostic(LiveProviderDiagnosticCodes.EmptyResponse));
            return observations.AsReadOnly();
        }

        foreach (ThemeParksWikiLiveDataItem item in response.LiveData)
        {
            ExternalLiveObservation? observation = NormalizeItem(item, diagnostics);
            if (observation is not null)
            {
                observations.Add(observation);
            }
        }

        return observations.AsReadOnly();
    }

    private static ExternalLiveObservation? NormalizeItem(
        ThemeParksWikiLiveDataItem? item,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        string? externalTargetId = NormalizeOptional(item?.Id);
        if (item is null
            || externalTargetId is null
            || string.IsNullOrWhiteSpace(item.Name)
            || !TryMapTargetType(item.EntityType, out LiveTargetType targetType)
            || !TryParseUtc(item.LastUpdated, out DateTime sourceUpdatedAtUtc))
        {
            string code = item is not null
                && !string.IsNullOrWhiteSpace(item.EntityType)
                && !TryMapTargetType(item.EntityType, out LiveTargetType _)
                    ? LiveProviderDiagnosticCodes.UnsupportedEntityType
                    : LiveProviderDiagnosticCodes.InvalidObservation;
            diagnostics.Add(new LiveProviderDiagnostic(code, externalTargetId));
            return null;
        }

        LiveOperationalStatus status = MapStatus(item.Status, externalTargetId, diagnostics);
        IReadOnlyCollection<LiveQueueObservation> queues = NormalizeQueues(
            item.Queue,
            externalTargetId,
            diagnostics);

        try
        {
            return new ExternalLiveObservation(
                externalTargetId,
                item.Name,
                targetType,
                status,
                sourceUpdatedAtUtc,
                queues);
        }
        catch (LiveDataValidationException)
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.InvalidObservation,
                externalTargetId));
            return null;
        }
    }

    private static LiveOperationalStatus MapStatus(
        string? rawStatus,
        string externalTargetId,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        LiveOperationalStatus status = rawStatus?.Trim().ToUpperInvariant() switch
        {
            "OPERATING" => LiveOperationalStatus.Open,
            "DOWN" => LiveOperationalStatus.Down,
            "CLOSED" => LiveOperationalStatus.Closed,
            "REFURBISHMENT" => LiveOperationalStatus.Maintenance,
            _ => LiveOperationalStatus.Unknown,
        };
        if (status == LiveOperationalStatus.Unknown)
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.UnknownStatus,
                externalTargetId,
                "status"));
        }

        return status;
    }

    private static IReadOnlyCollection<LiveQueueObservation> NormalizeQueues(
        JsonElement? queueElement,
        string externalTargetId,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        List<LiveQueueObservation> queues = new List<LiveQueueObservation>();
        if (!queueElement.HasValue
            || queueElement.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return queues.AsReadOnly();
        }

        if (queueElement.Value.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.InvalidQueueValue,
                externalTargetId,
                "queue"));
            return queues.AsReadOnly();
        }

        HashSet<LiveQueueKind> seenKinds = new HashSet<LiveQueueKind>();
        foreach (JsonProperty queueProperty in queueElement.Value.EnumerateObject())
        {
            if (!TryMapQueueKind(queueProperty.Name, out LiveQueueKind kind))
            {
                diagnostics.Add(new LiveProviderDiagnostic(
                    LiveProviderDiagnosticCodes.UnknownQueueKind,
                    externalTargetId,
                    queueProperty.Name));
                continue;
            }

            if (!seenKinds.Add(kind) || queueProperty.Value.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(new LiveProviderDiagnostic(
                    LiveProviderDiagnosticCodes.InvalidQueueValue,
                    externalTargetId,
                    queueProperty.Name));
                continue;
            }

            LiveQueueObservation? queue = NormalizeQueue(
                kind,
                queueProperty.Value,
                externalTargetId,
                diagnostics);
            if (queue is not null)
            {
                queues.Add(queue);
            }
        }

        return queues.AsReadOnly();
    }

    private static LiveQueueObservation? NormalizeQueue(
        LiveQueueKind kind,
        JsonElement value,
        string externalTargetId,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        if (!HasRequiredQueueProperties(kind, value))
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.InvalidQueueValue,
                externalTargetId,
                kind.ToString()));
            return null;
        }

        int? waitTimeMinutes = kind == LiveQueueKind.BoardingGroup
            ? ReadOptionalInt(value, "estimatedWait", externalTargetId, diagnostics)
            : ReadOptionalInt(value, "waitTime", externalTargetId, diagnostics);
        LiveQueueAvailability availability = kind switch
        {
            LiveQueueKind.ReturnTime or LiveQueueKind.PaidReturnTime =>
                MapReturnTimeState(ReadOptionalString(value, "state"), externalTargetId, diagnostics),
            LiveQueueKind.BoardingGroup =>
                MapBoardingGroupState(ReadOptionalString(value, "allocationStatus"), externalTargetId, diagnostics),
            _ => LiveQueueAvailability.Unspecified,
        };

        DateTime? returnStartUtc = ReadOptionalUtc(
            value,
            "returnStart",
            externalTargetId,
            diagnostics);
        DateTime? returnEndUtc = ReadOptionalUtc(
            value,
            "returnEnd",
            externalTargetId,
            diagnostics);
        int? currentGroupStart = ReadOptionalInt(
            value,
            "currentGroupStart",
            externalTargetId,
            diagnostics);
        int? currentGroupEnd = ReadOptionalInt(
            value,
            "currentGroupEnd",
            externalTargetId,
            diagnostics);
        DateTime? nextAllocationUtc = ReadOptionalUtc(
            value,
            "nextAllocationTime",
            externalTargetId,
            diagnostics);
        ReadPrice(value, externalTargetId, diagnostics, out long? priceMinorUnits, out string? currencyCode);

        try
        {
            return new LiveQueueObservation(
                kind,
                waitTimeMinutes,
                kind == LiveQueueKind.BoardingGroup,
                availability,
                returnStartUtc,
                returnEndUtc,
                currentGroupStart,
                currentGroupEnd,
                nextAllocationUtc,
                priceMinorUnits,
                currencyCode);
        }
        catch (LiveDataValidationException)
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.InvalidQueueValue,
                externalTargetId,
                kind.ToString()));
            return null;
        }
    }

    private static int? ReadOptionalInt(
        JsonElement parent,
        string propertyName,
        string externalTargetId,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int parsedValue))
        {
            return parsedValue;
        }

        diagnostics.Add(new LiveProviderDiagnostic(
            LiveProviderDiagnosticCodes.InvalidQueueValue,
            externalTargetId,
            propertyName));
        return null;
    }

    private static DateTime? ReadOptionalUtc(
        JsonElement parent,
        string propertyName,
        string externalTargetId,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        string? value = ReadOptionalString(parent, propertyName);
        if (value is null)
        {
            return null;
        }

        if (TryParseUtc(value, out DateTime parsedValue))
        {
            return parsedValue;
        }

        diagnostics.Add(new LiveProviderDiagnostic(
            LiveProviderDiagnosticCodes.InvalidQueueValue,
            externalTargetId,
            propertyName));
        return null;
    }

    private static string? ReadOptionalString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind == JsonValueKind.Null
            || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return NormalizeOptional(value.GetString());
    }

    private static void ReadPrice(
        JsonElement parent,
        string externalTargetId,
        ICollection<LiveProviderDiagnostic> diagnostics,
        out long? priceMinorUnits,
        out string? currencyCode)
    {
        priceMinorUnits = null;
        currencyCode = null;
        if (!parent.TryGetProperty("price", out JsonElement price)
            || price.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (price.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.InvalidQueueValue,
                externalTargetId,
                "price"));
            return;
        }

        currencyCode = ReadOptionalString(price, "currency");
        if (price.TryGetProperty("amount", out JsonElement amount)
            && amount.ValueKind != JsonValueKind.Null)
        {
            if (amount.ValueKind == JsonValueKind.Number && amount.TryGetInt64(out long parsedAmount))
            {
                priceMinorUnits = parsedAmount;
            }
            else
            {
                diagnostics.Add(new LiveProviderDiagnostic(
                    LiveProviderDiagnosticCodes.InvalidQueueValue,
                    externalTargetId,
                    "price.amount"));
            }
        }
    }

    private static LiveQueueAvailability MapReturnTimeState(
        string? value,
        string externalTargetId,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        LiveQueueAvailability availability = value?.ToUpperInvariant() switch
        {
            "AVAILABLE" => LiveQueueAvailability.Available,
            "TEMP_FULL" => LiveQueueAvailability.TemporarilyFull,
            "FINISHED" => LiveQueueAvailability.Finished,
            null => LiveQueueAvailability.Unspecified,
            _ => LiveQueueAvailability.Unknown,
        };
        AddUnknownQueueStateDiagnostic(availability, externalTargetId, diagnostics);
        return availability;
    }

    private static LiveQueueAvailability MapBoardingGroupState(
        string? value,
        string externalTargetId,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        LiveQueueAvailability availability = value?.ToUpperInvariant() switch
        {
            "AVAILABLE" => LiveQueueAvailability.Available,
            "PAUSED" => LiveQueueAvailability.Paused,
            "CLOSED" => LiveQueueAvailability.Closed,
            null => LiveQueueAvailability.Unspecified,
            _ => LiveQueueAvailability.Unknown,
        };
        AddUnknownQueueStateDiagnostic(availability, externalTargetId, diagnostics);
        return availability;
    }

    private static void AddUnknownQueueStateDiagnostic(
        LiveQueueAvailability availability,
        string externalTargetId,
        ICollection<LiveProviderDiagnostic> diagnostics)
    {
        if (availability == LiveQueueAvailability.Unknown)
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.UnknownQueueState,
                externalTargetId,
                "queue.state"));
        }
    }

    private static bool TryMapTargetType(string? value, out LiveTargetType targetType)
    {
        targetType = value?.Trim().ToUpperInvariant() switch
        {
            "PARK" => LiveTargetType.Park,
            "ATTRACTION" or "RESTAURANT" or "HOTEL" or "SHOW" => LiveTargetType.ParkItem,
            _ => default,
        };
        return targetType != default;
    }

    private static bool TryMapQueueKind(string value, out LiveQueueKind kind)
    {
        kind = value.ToUpperInvariant() switch
        {
            "STANDBY" => LiveQueueKind.Standby,
            "SINGLE_RIDER" => LiveQueueKind.SingleRider,
            "RETURN_TIME" => LiveQueueKind.ReturnTime,
            "PAID_RETURN_TIME" => LiveQueueKind.PaidReturnTime,
            "BOARDING_GROUP" => LiveQueueKind.BoardingGroup,
            "PAID_STANDBY" => LiveQueueKind.PaidStandby,
            _ => default,
        };
        return kind != default;
    }

    private static bool HasRequiredQueueProperties(LiveQueueKind kind, JsonElement value)
    {
        return kind switch
        {
            LiveQueueKind.Standby => true,
            LiveQueueKind.SingleRider or LiveQueueKind.PaidStandby =>
                value.TryGetProperty("waitTime", out JsonElement _),
            LiveQueueKind.ReturnTime =>
                value.TryGetProperty("state", out JsonElement _)
                && value.TryGetProperty("returnStart", out JsonElement _)
                && value.TryGetProperty("returnEnd", out JsonElement _),
            LiveQueueKind.PaidReturnTime =>
                value.TryGetProperty("state", out JsonElement _)
                && value.TryGetProperty("returnStart", out JsonElement _)
                && value.TryGetProperty("returnEnd", out JsonElement _)
                && value.TryGetProperty("price", out JsonElement _),
            LiveQueueKind.BoardingGroup =>
                value.TryGetProperty("allocationStatus", out JsonElement _)
                && value.TryGetProperty("currentGroupStart", out JsonElement _)
                && value.TryGetProperty("currentGroupEnd", out JsonElement _)
                && value.TryGetProperty("nextAllocationTime", out JsonElement _)
                && value.TryGetProperty("estimatedWait", out JsonElement _),
            _ => false,
        };
    }

    private static bool TryParseUtc(string? value, out DateTime utcValue)
    {
        bool parsed = DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
            out DateTimeOffset parsedValue);
        utcValue = parsed ? parsedValue.UtcDateTime : default;
        return parsed;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
