using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.LiveData;

public sealed class ExternalLiveTargetDescriptor
{
    private const int MaximumDisplayNameLength = 300;

    public ExternalLiveTargetDescriptor(
        LiveTargetType type,
        string id,
        string? parentId,
        string displayName,
        string? parentDisplayName,
        string countryCode)
    {
        ValidateEnum(type, nameof(type));
        string normalizedId = IdentifierRules.NormalizeRequired(id, nameof(id));
        string? normalizedParentId = NormalizeOptionalIdentifier(parentId, nameof(parentId));
        string normalizedDisplayName = NormalizeDisplayName(displayName, nameof(displayName));
        string? normalizedParentDisplayName = NormalizeOptionalDisplayName(
            parentDisplayName,
            nameof(parentDisplayName));
        string normalizedCountryCode = NormalizeCountryCode(countryCode, nameof(countryCode));
        ValidateParent(type, normalizedParentId, normalizedParentDisplayName);

        this.Type = type;
        this.Id = normalizedId;
        this.ParentId = normalizedParentId;
        this.DisplayName = normalizedDisplayName;
        this.ParentDisplayName = normalizedParentDisplayName;
        this.CountryCode = normalizedCountryCode;
    }

    public LiveTargetType Type { get; }

    public string Id { get; }

    public string? ParentId { get; }

    public string DisplayName { get; }

    public string? ParentDisplayName { get; }

    public string CountryCode { get; }

    private static void ValidateParent(
        LiveTargetType type,
        string? parentId,
        string? parentDisplayName)
    {
        bool isValid = type switch
        {
            LiveTargetType.Park => parentId is null && parentDisplayName is null,
            LiveTargetType.ParkItem => parentId is not null && parentDisplayName is not null,
            _ => false,
        };
        if (!isValid)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidMapping,
                "An external park item requires its parent park evidence, while a park cannot have one.");
        }
    }

    private static string? NormalizeOptionalIdentifier(string? value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : IdentifierRules.NormalizeRequired(value, parameterName);
    }

    private static string NormalizeDisplayName(string? value, string parameterName)
    {
        return NormalizeOptionalDisplayName(value, parameterName)
            ?? throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "An external live target display name is required.",
                parameterName);
    }

    private static string? NormalizeOptionalDisplayName(string? value, string parameterName)
    {
        string? normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalizedValue is not null
            && (normalizedValue.Length > MaximumDisplayNameLength
                || normalizedValue.Any(char.IsControl)))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "An external live target display name is invalid.",
                parameterName);
        }

        return normalizedValue;
    }

    internal static string NormalizeCountryCode(string? value, string parameterName)
    {
        string normalizedValue = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizedValue.Length != 2 || normalizedValue.Any(static character => !char.IsLetter(character)))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidMapping,
                "A live mapping requires an ISO alpha-2 country code.",
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
                "An external live target contains an invalid type.",
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
