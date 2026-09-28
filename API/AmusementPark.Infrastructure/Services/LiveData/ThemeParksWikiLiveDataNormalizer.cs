using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Infrastructure.Services.LiveData;

internal static class ThemeParksWikiLiveDataNormalizer
{
    private static readonly Regex Rfc3339Pattern = new Regex(
        "^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(?:\\.\\d{1,7})?(?:Z|[+-]\\d{2}:\\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking,
        TimeSpan.FromMilliseconds(100));

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
        if (item is null)
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.InvalidObservation));
            return null;
        }

        if (!TryReadJsonString(item.Id, false, out string? rawExternalTargetId)
            || !TryNormalizeExternalTargetId(rawExternalTargetId, out string externalTargetId))
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.InvalidObservation));
            return null;
        }

        if (!TryReadJsonString(item.Name, false, out string? name)
            || !TryReadJsonString(item.EntityType, false, out string? entityType)
            || !TryReadJsonString(item.Status, true, out string? status)
            || !TryReadJsonString(item.LastUpdated, false, out string? lastUpdated))
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.InvalidObservation,
                externalTargetId));
            return null;
        }

        if (string.IsNullOrWhiteSpace(name)
            || !TryMapTargetType(entityType, out LiveTargetType targetType)
            || !TryParseUtc(lastUpdated, out DateTime sourceUpdatedAtUtc))
        {
            string code = !string.IsNullOrWhiteSpace(entityType)
                && !TryMapTargetType(entityType, out LiveTargetType _)
                    ? LiveProviderDiagnosticCodes.UnsupportedEntityType
                    : LiveProviderDiagnosticCodes.InvalidObservation;
            diagnostics.Add(new LiveProviderDiagnostic(code, externalTargetId));
            return null;
        }

        LiveOperationalStatus operationalStatus = MapStatus(status, externalTargetId, diagnostics);
        IReadOnlyCollection<LiveQueueObservation> queues = NormalizeQueues(
            item.Queue,
            externalTargetId,
            diagnostics);
        if (operationalStatus == LiveOperationalStatus.Closed
            && queues.Any(static queue => queue.WaitTimeMinutes.HasValue))
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.StatusQueueConflict,
                externalTargetId,
                "queue.waitTime"));
        }

        try
        {
            return new ExternalLiveObservation(
                externalTargetId,
                name,
                targetType,
                operationalStatus,
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
        catch (IdentifierValidationException)
        {
            diagnostics.Add(new LiveProviderDiagnostic(
                LiveProviderDiagnosticCodes.InvalidObservation));
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
                    NormalizeDiagnosticField(queueProperty.Name)));
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

        int? waitTimeMinutes = kind switch
        {
            LiveQueueKind.Standby or LiveQueueKind.SingleRider or LiveQueueKind.PaidStandby =>
                ReadOptionalInt(value, "waitTime", externalTargetId, diagnostics),
            LiveQueueKind.BoardingGroup =>
                ReadOptionalInt(value, "estimatedWait", externalTargetId, diagnostics),
            _ => null,
        };
        LiveQueueAvailability availability = kind switch
        {
            LiveQueueKind.ReturnTime or LiveQueueKind.PaidReturnTime =>
                MapReturnTimeState(ReadOptionalString(value, "state"), externalTargetId, diagnostics),
            LiveQueueKind.BoardingGroup =>
                MapBoardingGroupState(ReadOptionalString(value, "allocationStatus"), externalTargetId, diagnostics),
            _ => LiveQueueAvailability.Unspecified,
        };

        bool isReturnTime = kind is LiveQueueKind.ReturnTime or LiveQueueKind.PaidReturnTime;
        DateTime? returnStartUtc = isReturnTime
            ? ReadOptionalUtc(value, "returnStart", externalTargetId, diagnostics)
            : null;
        DateTime? returnEndUtc = isReturnTime
            ? ReadOptionalUtc(value, "returnEnd", externalTargetId, diagnostics)
            : null;
        int? currentGroupStart = kind == LiveQueueKind.BoardingGroup
            ? ReadOptionalInt(value, "currentGroupStart", externalTargetId, diagnostics)
            : null;
        int? currentGroupEnd = kind == LiveQueueKind.BoardingGroup
            ? ReadOptionalInt(value, "currentGroupEnd", externalTargetId, diagnostics)
            : null;
        DateTime? nextAllocationUtc = kind == LiveQueueKind.BoardingGroup
            ? ReadOptionalUtc(value, "nextAllocationTime", externalTargetId, diagnostics)
            : null;
        long? priceMinorUnits = null;
        string? currencyCode = null;
        if (kind == LiveQueueKind.PaidReturnTime)
        {
            ReadPrice(
                value,
                externalTargetId,
                diagnostics,
                out priceMinorUnits,
                out currencyCode);
        }

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
        if (!parent.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String
            && TryParseUtc(value.GetString(), out DateTime parsedValue))
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
                HasNullablePropertyOfKind(value, "waitTime", JsonValueKind.Number),
            LiveQueueKind.ReturnTime =>
                HasNullablePropertyOfKind(value, "state", JsonValueKind.String)
                && HasNullablePropertyOfKind(value, "returnStart", JsonValueKind.String)
                && HasNullablePropertyOfKind(value, "returnEnd", JsonValueKind.String),
            LiveQueueKind.PaidReturnTime =>
                HasNullablePropertyOfKind(value, "state", JsonValueKind.String)
                && HasNullablePropertyOfKind(value, "returnStart", JsonValueKind.String)
                && HasNullablePropertyOfKind(value, "returnEnd", JsonValueKind.String)
                && HasValidRequiredPrice(value),
            LiveQueueKind.BoardingGroup =>
                HasNullablePropertyOfKind(value, "allocationStatus", JsonValueKind.String)
                && HasNullablePropertyOfKind(value, "currentGroupStart", JsonValueKind.Number)
                && HasNullablePropertyOfKind(value, "currentGroupEnd", JsonValueKind.Number)
                && HasNullablePropertyOfKind(value, "nextAllocationTime", JsonValueKind.String)
                && HasNullablePropertyOfKind(value, "estimatedWait", JsonValueKind.Number),
            _ => false,
        };
    }

    private static bool HasNullablePropertyOfKind(
        JsonElement parent,
        string propertyName,
        JsonValueKind expectedKind)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        return value.ValueKind == expectedKind
            && (expectedKind != JsonValueKind.String
                || !string.IsNullOrWhiteSpace(value.GetString()));
    }

    private static bool HasValidRequiredPrice(JsonElement parent)
    {
        if (!parent.TryGetProperty("price", out JsonElement price))
        {
            return false;
        }

        if (price.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        return price.ValueKind == JsonValueKind.Object
            && HasNullablePropertyOfKind(price, "amount", JsonValueKind.Number)
            && price.TryGetProperty("currency", out JsonElement currency)
            && currency.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(currency.GetString());
    }

    private static bool TryParseUtc(string? value, out DateTime utcValue)
    {
        if (value is null || value.Length > 40 || !Rfc3339Pattern.IsMatch(value))
        {
            utcValue = default;
            return false;
        }

        bool parsed = DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTimeOffset parsedValue);
        utcValue = parsed ? parsedValue.UtcDateTime : default;
        return parsed;
    }

    private static bool TryReadJsonString(
        JsonElement? element,
        bool allowNull,
        out string? value)
    {
        value = null;
        if (!element.HasValue
            || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return allowNull;
        }

        if (element.Value.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.Value.GetString();
        return true;
    }

    private static bool TryNormalizeExternalTargetId(string? value, out string externalTargetId)
    {
        try
        {
            externalTargetId = IdentifierRules.NormalizeRequired(value, nameof(value));
            return true;
        }
        catch (IdentifierValidationException)
        {
            externalTargetId = string.Empty;
            return false;
        }
    }

    private static string? NormalizeDiagnosticField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl))
        {
            return null;
        }

        string normalizedValue = value.Trim();
        return normalizedValue.Length <= 100
            ? normalizedValue
            : normalizedValue[..100];
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
