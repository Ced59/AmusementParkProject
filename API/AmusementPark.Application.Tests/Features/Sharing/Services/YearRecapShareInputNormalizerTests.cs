using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class YearRecapShareInputNormalizerTests
{
    [Fact]
    public void Normalize_ShouldTrimTheExplicitPublicCaptionAndCreateAStableFingerprint()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.YearRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.PublicCaption });

        ApplicationResult<YearRecapShareInput> result = YearRecapShareInputNormalizer.Normalize(
            new YearRecapShareInput("  Une année mémorable  "),
            policy);

        Assert.True(result.IsSuccess);
        Assert.Equal("Une année mémorable", result.Value!.PublicCaption);
        Assert.Equal(
            YearRecapShareInputNormalizer.CreateFingerprint(result.Value),
            YearRecapShareInputNormalizer.CreateFingerprint(
                new YearRecapShareInput("Une année mémorable")));
    }

    [Fact]
    public void Normalize_WhenCaptionWasNotApproved_ShouldRejectIt()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.YearRecap,
            ShareDatePrecision.Year,
            Array.Empty<ShareContentField>());

        ApplicationResult<YearRecapShareInput> result = YearRecapShareInputNormalizer.Normalize(
            new YearRecapShareInput("Souvenir public"),
            policy);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.year-recap-selection-invalid");
    }
}
