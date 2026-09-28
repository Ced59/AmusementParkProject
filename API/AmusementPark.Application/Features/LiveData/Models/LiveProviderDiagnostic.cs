using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed class LiveProviderDiagnostic
{
    public LiveProviderDiagnostic(string code, string? externalTargetId = null, string? field = null)
    {
        string normalizedCode = code?.Trim() ?? string.Empty;
        if (normalizedCode.Length is 0 or > 100 || normalizedCode.Any(char.IsControl))
        {
            throw new ArgumentException(
                "A live provider diagnostic code is required and cannot exceed 100 characters.",
                nameof(code));
        }

        this.Code = normalizedCode;
        this.ExternalTargetId = NormalizeOptional(
            externalTargetId,
            IdentifierRules.MaximumLength,
            nameof(externalTargetId));
        this.Field = NormalizeOptional(field, 100, nameof(field));
    }

    public string Code { get; }

    public string? ExternalTargetId { get; }

    public string? Field { get; }

    private static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength || normalizedValue.Any(char.IsControl))
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }
}
