using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class VisitRecapShareInputNormalizerTests
{
    [Fact]
    public void Normalize_ShouldCanonicalizeSelectionAndKeepThePublicCaptionSeparate()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Month,
            new[] { ShareContentField.RideCount, ShareContentField.PublicCaption });

        AmusementPark.Application.Errors.ApplicationResult<VisitRecapShareInput> result =
            VisitRecapShareInputNormalizer.Normalize(
                new VisitRecapShareInput(
                    new[] { " item-b ", "item-a", "item-a", string.Empty },
                    "  Public memory only  "),
                policy);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "item-a", "item-b" }, result.Value!.SelectedParkItemIds);
        Assert.Equal("Public memory only", result.Value.PublicCaption);
    }

    [Fact]
    public void Normalize_WhenCaptionWasNotExplicitlyAuthorized_ShouldRejectIt()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.RideCount });

        AmusementPark.Application.Errors.ApplicationResult<VisitRecapShareInput> result =
            VisitRecapShareInputNormalizer.Normalize(
                new VisitRecapShareInput(null, "A public text"),
                policy);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.visit-recap-selection-invalid");
    }

    [Fact]
    public void CreateFingerprint_ShouldBeStableForEquivalentSelectionsAndDifferentForAnotherCaption()
    {
        string first = VisitRecapShareInputNormalizer.CreateFingerprint(
            new VisitRecapShareInput(new[] { "item-a", "item-b" }, "Memory"));
        string equivalent = VisitRecapShareInputNormalizer.CreateFingerprint(
            new VisitRecapShareInput(new[] { "item-a", "item-b" }, "Memory"));
        string changed = VisitRecapShareInputNormalizer.CreateFingerprint(
            new VisitRecapShareInput(new[] { "item-a", "item-b" }, "Another memory"));

        Assert.Equal(first, equivalent);
        Assert.NotEqual(first, changed);
        Assert.Equal(64, first.Length);
    }
}
