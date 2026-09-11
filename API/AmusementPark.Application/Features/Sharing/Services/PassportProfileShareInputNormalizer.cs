using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class PassportProfileShareInputNormalizer
{
    public const int MaximumSelectedYears = 100;
    public const int MaximumSelectedParks = 250;
    public const int MaximumSelectedRatings = 100;

    public static ApplicationResult<PassportProfileShareInput> Normalize(
        PassportProfileShareInput? input,
        ShareContentPolicy contentPolicy)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        PassportProfileShareInput value = input ?? new PassportProfileShareInput(
            Array.Empty<int>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            ShareVisibility.Unlisted,
            false);
        int[] years = (value.SelectedYears ?? Array.Empty<int>())
            .Distinct()
            .OrderByDescending(static year => year)
            .ToArray();
        string[] parkIds = NormalizeIdentifiers(value.SelectedParkIds);
        string[] ratingKeys = NormalizeIdentifiers(value.SelectedRatingKeys);
        string normalizedCaption = value.PublicCaption?.Trim() ?? string.Empty;
        string? caption = normalizedCaption.Length == 0 ? null : normalizedCaption;
        bool invalid = years.Length > MaximumSelectedYears
            || years.Any(static year => year < DateOnly.MinValue.Year || year > DateOnly.MaxValue.Year)
            || parkIds.Length > MaximumSelectedParks
            || ratingKeys.Length > MaximumSelectedRatings
            || value.Visibility is not ShareVisibility.Unlisted and not ShareVisibility.Public
            || caption?.Length > VisitRecapShareInputNormalizer.MaximumCaptionLength
            || caption?.Any(static character => char.IsControl(character)
                && character is not '\r' and not '\n' and not '\t') == true
            || caption is not null && !contentPolicy.Includes(ShareContentField.PublicCaption)
            || ratingKeys.Length > 0 && !contentPolicy.Includes(ShareContentField.GlobalRatings);
        if (invalid)
        {
            return ApplicationResult<PassportProfileShareInput>.Failure(
                SharingApplicationErrors.InvalidPassportProfileSelection());
        }

        return ApplicationResult<PassportProfileShareInput>.Success(
            new PassportProfileShareInput(
                years,
                parkIds,
                ratingKeys,
                caption,
                value.Visibility,
                value.AllowsComparisons));
    }

    public static string CreateFingerprint(PassportProfileShareInput normalizedInput)
    {
        ArgumentNullException.ThrowIfNull(normalizedInput);
        StringBuilder canonical = new StringBuilder();
        Append(canonical, normalizedInput.Visibility.ToString());
        Append(canonical, normalizedInput.AllowsComparisons);
        Append(canonical, normalizedInput.PublicCaption);
        foreach (int year in normalizedInput.SelectedYears ?? Array.Empty<int>())
        {
            Append(canonical, year);
        }

        foreach (string parkId in normalizedInput.SelectedParkIds ?? Array.Empty<string>())
        {
            Append(canonical, parkId);
        }

        foreach (string ratingKey in normalizedInput.SelectedRatingKeys ?? Array.Empty<string>())
        {
            Append(canonical, ratingKey);
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static string[] NormalizeIdentifiers(IEnumerable<string>? values)
    {
        return (values ?? Array.Empty<string>())
            .Select(static value => value?.Trim() ?? string.Empty)
            .Where(static value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static void Append(StringBuilder target, object? value)
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        target.Append(text.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(text);
    }
}
