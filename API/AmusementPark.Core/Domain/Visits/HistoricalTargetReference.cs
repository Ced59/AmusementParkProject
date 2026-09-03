using System.Globalization;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Repli minimal conservé uniquement lorsqu'une cible référencée doit être supprimée.
/// </summary>
public sealed record HistoricalTargetReference
{
    public const int MaximumNameLength = 200;

    public const int MaximumCategoryLength = 100;

    public HistoricalTargetReference(string name, string? category)
    {
        this.Name = NormalizeRequired(name, MaximumNameLength, nameof(name));
        this.Category = NormalizeOptional(category, MaximumCategoryLength, nameof(category));
    }

    public string Name { get; }

    public string? Category { get; }

    private static string NormalizeRequired(string? value, int maximumLength, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.HistoricalTargetNameRequired,
                "A historical target name is required.",
                parameterName);
        }

        ValidateText(normalizedValue, maximumLength, parameterName);
        return normalizedValue;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            return null;
        }

        ValidateText(normalizedValue, maximumLength, parameterName);
        return normalizedValue;
    }

    private static void ValidateText(string value, int maximumLength, string parameterName)
    {
        if (value.Length > maximumLength)
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.HistoricalTargetTextTooLong,
                $"The historical target text cannot exceed {maximumLength} characters.",
                parameterName);
        }

        foreach (char character in value)
        {
            UnicodeCategory category = char.GetUnicodeCategory(character);
            if (char.IsControl(character)
                || category is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
            {
                throw CreateValidationException(
                    RideOccurrenceErrorCodes.HistoricalTargetControlCharacter,
                    "Historical target text cannot contain control characters.",
                    parameterName);
            }
        }
    }

    private static RideOccurrenceValidationException CreateValidationException(
        string errorCode,
        string message,
        string parameterName)
    {
        return new RideOccurrenceValidationException(errorCode, message, parameterName);
    }
}
