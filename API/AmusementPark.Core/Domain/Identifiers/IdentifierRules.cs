namespace AmusementPark.Core.Domain.Identifiers;

/// <summary>
/// Règles communes des nouveaux identifiants métier persistés sous forme de chaînes opaques.
/// </summary>
public static class IdentifierRules
{
    /// <summary>
    /// Longueur maximale compatible avec les identifiants existants et les futures références externes.
    /// </summary>
    public const int MaximumLength = 256;

    public static string NormalizeRequired(string? value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);

        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            throw new IdentifierValidationException(
                IdentifierErrorCodes.Required,
                "A non-empty identifier is required.",
                parameterName);
        }

        if (value is not null)
        {
            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    throw new IdentifierValidationException(
                        IdentifierErrorCodes.ControlCharacter,
                        "The identifier cannot contain control characters.",
                        parameterName);
                }
            }
        }

        if (normalizedValue.Length > MaximumLength)
        {
            throw new IdentifierValidationException(
                IdentifierErrorCodes.TooLong,
                $"The identifier cannot exceed {MaximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }
}
