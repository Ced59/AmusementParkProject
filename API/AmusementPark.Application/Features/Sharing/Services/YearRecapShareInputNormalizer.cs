using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class YearRecapShareInputNormalizer
{
    public static ApplicationResult<YearRecapShareInput> Normalize(
        YearRecapShareInput? input,
        ShareContentPolicy contentPolicy)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        string normalized = input?.PublicCaption?.Trim() ?? string.Empty;
        string? caption = normalized.Length == 0 ? null : normalized;
        if (caption?.Length > VisitRecapShareInputNormalizer.MaximumCaptionLength
            || caption?.Any(static character => char.IsControl(character)
                && character is not '\r' and not '\n' and not '\t') == true
            || caption is not null && !contentPolicy.Includes(ShareContentField.PublicCaption))
        {
            return ApplicationResult<YearRecapShareInput>.Failure(
                SharingApplicationErrors.InvalidYearRecapSelection());
        }

        return ApplicationResult<YearRecapShareInput>.Success(
            new YearRecapShareInput(caption));
    }

    public static string CreateFingerprint(YearRecapShareInput normalizedInput)
    {
        ArgumentNullException.ThrowIfNull(normalizedInput);
        string value = normalizedInput.PublicCaption ?? string.Empty;
        string canonical = string.Concat(
            value.Length.ToString(CultureInfo.InvariantCulture),
            ":",
            value);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
