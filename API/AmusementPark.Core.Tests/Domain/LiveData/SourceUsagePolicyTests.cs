using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class SourceUsagePolicyTests
{
    private static readonly DateTime ReviewDateUtc = new(
        2026,
        9,
        28,
        12,
        0,
        0,
        DateTimeKind.Utc);

    [Fact]
    public void Constructor_ShouldNormalizePolicyEvidence()
    {
        SourceUsagePolicy policy = new SourceUsagePolicy(
            "  terms-2026-09  ",
            "  https://example.org/terms  ",
            true,
            true,
            false,
            true,
            "  live.attribution.example  ",
            ReviewDateUtc);

        Assert.Equal("terms-2026-09", policy.Version);
        Assert.Equal("https://example.org/terms", policy.TermsUrl);
        Assert.Equal("live.attribution.example", policy.AttributionTemplateKey);
        Assert.True(policy.CommercialUseAllowed);
        Assert.True(policy.HistoricalStorageAllowed);
        Assert.False(policy.RedistributionAllowed);
        Assert.Equal(ReviewDateUtc, policy.ReviewedAtUtc);
    }

    [Fact]
    public void Constructor_WhenAttributionIsRequiredWithoutTemplate_ShouldRejectPolicy()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new SourceUsagePolicy(
                "terms-1",
                "https://example.org/terms",
                true,
                false,
                false,
                true,
                null,
                ReviewDateUtc));

        Assert.Equal(LiveDataErrorCodes.MissingAttribution, exception.Code);
    }

    [Fact]
    public void Constructor_WhenTermsUrlIsNotHttps_ShouldRejectPolicy()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => CreatePolicy("http://example.org/terms", ReviewDateUtc));

        Assert.Equal(LiveDataErrorCodes.InvalidUri, exception.Code);
    }

    [Fact]
    public void Constructor_WhenReviewDateIsNotUtc_ShouldRejectPolicy()
    {
        DateTime localReviewDate = DateTime.SpecifyKind(ReviewDateUtc, DateTimeKind.Local);

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => CreatePolicy("https://example.org/terms", localReviewDate));

        Assert.Equal(LiveDataErrorCodes.InvalidTimestamp, exception.Code);
    }

    private static SourceUsagePolicy CreatePolicy(string termsUrl, DateTime reviewedAtUtc)
    {
        return new SourceUsagePolicy(
            "terms-1",
            termsUrl,
            true,
            false,
            false,
            false,
            null,
            reviewedAtUtc);
    }
}
