using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class VisitRecapShareInputNormalizer
{
    public const int MaximumCaptionLength = 500;

    public const int MaximumSelectedItemCount = 100;

    public static ApplicationResult<VisitRecapShareInput> Normalize(
        VisitRecapShareInput? input,
        ShareContentPolicy contentPolicy)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        IReadOnlyCollection<string>? selectedIds = input?.SelectedParkItemIds is null
            ? null
            : input.SelectedParkItemIds
                .Select(static value => value?.Trim() ?? string.Empty)
                .Where(static value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static value => value, StringComparer.Ordinal)
                .ToArray();
        if (selectedIds is not null
            && (selectedIds.Count > MaximumSelectedItemCount
                || selectedIds.Any(static value => value.Length > 256)))
        {
            return Invalid();
        }

        string? caption = NormalizeOptional(input?.PublicCaption);
        if (caption?.Length > MaximumCaptionLength
            || caption?.Any(static character => char.IsControl(character)
                && character is not '\r' and not '\n' and not '\t') == true
            || caption is not null && !contentPolicy.Includes(ShareContentField.PublicCaption))
        {
            return Invalid();
        }

        return ApplicationResult<VisitRecapShareInput>.Success(
            new VisitRecapShareInput(selectedIds, caption));
    }

    public static string CreateFingerprint(VisitRecapShareInput normalizedInput)
    {
        ArgumentNullException.ThrowIfNull(normalizedInput);
        StringBuilder canonical = new StringBuilder();
        Append(canonical, normalizedInput.SelectedParkItemIds is null ? "all" : "selected");
        foreach (string parkItemId in normalizedInput.SelectedParkItemIds
                     ?? Array.Empty<string>())
        {
            Append(canonical, parkItemId);
        }

        Append(canonical, normalizedInput.PublicCaption ?? string.Empty);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static ApplicationResult<VisitRecapShareInput> Invalid()
    {
        return ApplicationResult<VisitRecapShareInput>.Failure(
            SharingApplicationErrors.InvalidVisitRecapSelection());
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }

    private static void Append(StringBuilder canonical, string value)
    {
        canonical.Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value);
    }
}
