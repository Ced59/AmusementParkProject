using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileShareInputNormalizerTests
{
    [Fact]
    public void Normalize_ShouldCanonicalizeSelectionAndBindPrivacyChoicesToFingerprint()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            new[] { ShareContentField.PublicCaption, ShareContentField.GlobalRatings });
        PassportProfileShareInput input = new PassportProfileShareInput(
            new[] { 2025, 2026, 2025 },
            new[] { " park-2 ", "park-1", "park-2" },
            new[] { " rating-2 ", "rating-1" },
            "  Mes meilleurs souvenirs  ",
            ShareVisibility.Public,
            true);

        ApplicationResult<PassportProfileShareInput> result =
            PassportProfileShareInputNormalizer.Normalize(input, policy);

        Assert.True(result.IsSuccess);
        PassportProfileShareInput normalized = Assert.IsType<PassportProfileShareInput>(result.Value);
        Assert.Equal(new[] { 2026, 2025 }, normalized.SelectedYears);
        Assert.Equal(new[] { "park-1", "park-2" }, normalized.SelectedParkIds);
        Assert.Equal(new[] { "rating-1", "rating-2" }, normalized.SelectedRatingKeys);
        Assert.Equal("Mes meilleurs souvenirs", normalized.PublicCaption);
        Assert.NotEqual(
            PassportProfileShareInputNormalizer.CreateFingerprint(normalized),
            PassportProfileShareInputNormalizer.CreateFingerprint(normalized with
            {
                Visibility = ShareVisibility.Unlisted,
                AllowsComparisons = false,
            }));
    }

    [Fact]
    public void Normalize_WhenRankingWasNotApproved_ShouldRejectTechnicalSelectionKeys()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            Array.Empty<ShareContentField>());

        ApplicationResult<PassportProfileShareInput> result = PassportProfileShareInputNormalizer.Normalize(
            new PassportProfileShareInput(
                new[] { 2026 },
                new[] { "park-1" },
                new[] { "rating-key" },
                null,
                ShareVisibility.Unlisted,
                false),
            policy);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.passport-profile-selection-invalid");
    }
}
