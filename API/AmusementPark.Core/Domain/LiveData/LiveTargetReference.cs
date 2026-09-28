using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveTargetReference : IEquatable<LiveTargetReference>
{
    private const int MaximumDisplayNameLength = 300;

    public LiveTargetReference(
        LiveTargetType type,
        string id,
        string parkId,
        string displayName,
        string parkDisplayName,
        string countryCode)
    {
        ValidateEnum(type, nameof(type));
        string normalizedId = IdentifierRules.NormalizeRequired(id, nameof(id));
        string normalizedParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        string normalizedDisplayName = NormalizeDisplayName(displayName, nameof(displayName));
        string normalizedParkDisplayName = NormalizeDisplayName(
            parkDisplayName,
            nameof(parkDisplayName));
        string normalizedCountryCode = ExternalLiveTargetDescriptor.NormalizeCountryCode(
            countryCode,
            nameof(countryCode));
        bool targetHierarchyIsValid = type == LiveTargetType.Park
            ? string.Equals(normalizedId, normalizedParkId, StringComparison.Ordinal)
                && string.Equals(
                    normalizedDisplayName,
                    normalizedParkDisplayName,
                    StringComparison.Ordinal)
            : !string.Equals(normalizedId, normalizedParkId, StringComparison.Ordinal);
        if (!targetHierarchyIsValid)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidMapping,
                "A live target reference is inconsistent with its park hierarchy.");
        }

        this.Type = type;
        this.Id = normalizedId;
        this.ParkId = normalizedParkId;
        this.DisplayName = normalizedDisplayName;
        this.ParkDisplayName = normalizedParkDisplayName;
        this.CountryCode = normalizedCountryCode;
    }

    public LiveTargetType Type { get; }

    public string Id { get; }

    public string ParkId { get; }

    public string DisplayName { get; }

    public string ParkDisplayName { get; }

    public string CountryCode { get; }

    public bool Equals(LiveTargetReference? other)
    {
        return other is not null
            && this.Type == other.Type
            && string.Equals(this.Id, other.Id, StringComparison.Ordinal)
            && string.Equals(this.ParkId, other.ParkId, StringComparison.Ordinal)
            && string.Equals(this.CountryCode, other.CountryCode, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is LiveTargetReference other && this.Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Type, this.Id, this.ParkId, this.CountryCode);
    }

    private static string NormalizeDisplayName(string? value, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length is 0 or > MaximumDisplayNameLength
            || normalizedValue.Any(char.IsControl))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "A live target display name is required and cannot exceed 300 characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidEnum,
                "A live target reference contains an invalid type.",
                parameterName);
        }
    }

    private static LiveDataValidationException Invalid(
        string code,
        string message,
        string? parameterName = null)
    {
        return new LiveDataValidationException(code, message, parameterName);
    }
}
