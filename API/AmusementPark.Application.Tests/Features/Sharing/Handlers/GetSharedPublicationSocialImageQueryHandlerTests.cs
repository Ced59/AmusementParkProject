using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

public sealed class GetSharedPublicationSocialImageQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRenderOnlyMetricsAlreadyPresentInThePublicVisitSnapshot()
    {
        SharedVisitRecapResult recap = new SharedVisitRecapResult(
            new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc),
            new VisitRecapSharePreviewResult(
                "park-internal-id",
                "Denain Évasion",
                new VisitRecapShareDateResult(
                    2026,
                    7,
                    null,
                    ShareDatePrecision.Month,
                    false),
                8,
                null,
                Array.Empty<string>(),
                4.5,
                new VisitRecapShareHighlightResult("Le Galion", null, 5),
                null,
                Array.Empty<VisitRecapShareItemResult>(),
                null,
                false,
                false,
                false),
            7);
        Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>> visitHandler =
            new Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>>(MockBehavior.Strict);
        visitHandler.Setup(value => value.HandleAsync(
                It.Is<GetSharedVisitRecapQuery>(query => query.ShareId == "opaque-token"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharedVisitRecapResult>.Success(recap));
        Mock<IShareSocialImageRenderer> renderer = new Mock<IShareSocialImageRenderer>(MockBehavior.Strict);
        renderer.Setup(value => value.RenderAsync(
                It.Is<ShareSocialImageModel>(model =>
                    model.PublicationType == SharePublicationType.VisitRecap
                    && model.Language == "fr"
                    && model.Subject == "Denain Évasion"
                    && model.Date != null
                    && model.Date.Precision == ShareDatePrecision.Month
                    && model.Metrics.Count == 2
                    && model.Metrics.All(metric => metric.Kind != ShareSocialImageMetricKind.Rides)
                    && model.Highlight == "Le Galion"
                    && model.PublicationVersion == 7),
                CancellationToken.None))
            .ReturnsAsync(new ShareSocialImageRenderResult(
                new byte[] { 1, 2, 3 },
                "image/png",
                "texte alternatif",
                "\"etag\""));
        GetSharedPublicationSocialImageQueryHandler handler = CreateHandler(
            visitHandler,
            renderer);

        ApplicationResult<ShareSocialImageRenderResult> result = await handler.HandleAsync(
            new GetSharedPublicationSocialImageQuery(
                "opaque-token",
                SharePublicationType.VisitRecap,
                7,
                "FR"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("texte alternatif", result.Value!.AlternativeText);
        visitHandler.VerifyAll();
        renderer.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldBuildTheYearImageFromThePublishedAnnualStory()
    {
        SharedYearRecapResult recap = new SharedYearRecapResult(
            DateTime.UnixEpoch,
            new YearRecapSharePreviewResult(
                2026,
                3,
                4,
                1,
                0.25,
                18,
                null,
                null,
                Array.Empty<string>(),
                null,
                null,
                Array.Empty<YearRecapShareParkResult>(),
                new YearRecapShareHighlightResult("Le Galion", 5, 0, null, false),
                null,
                null,
                Array.Empty<YearRecapShareHighlightResult>(),
                "Une belle année",
                false,
                "year-v1",
                false),
            9);
        Mock<IQueryHandler<GetSharedYearRecapQuery, ApplicationResult<SharedYearRecapResult>>> yearHandler =
            new Mock<IQueryHandler<GetSharedYearRecapQuery, ApplicationResult<SharedYearRecapResult>>>(MockBehavior.Strict);
        yearHandler.Setup(value => value.HandleAsync(
                It.Is<GetSharedYearRecapQuery>(query => query.ShareId == "year-token"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharedYearRecapResult>.Success(recap));
        Mock<IShareSocialImageRenderer> renderer = new Mock<IShareSocialImageRenderer>(MockBehavior.Strict);
        renderer.Setup(value => value.RenderAsync(
                It.Is<ShareSocialImageModel>(model =>
                    model.PublicationType == SharePublicationType.YearRecap
                    && model.Year == 2026
                    && model.Subject == null
                    && model.Metrics.Select(metric => metric.Kind).SequenceEqual(new[]
                    {
                        ShareSocialImageMetricKind.Parks,
                        ShareSocialImageMetricKind.Visits,
                        ShareSocialImageMetricKind.Rides,
                    })
                    && model.Highlight == "Le Galion"
                    && model.PublicationVersion == 9),
                CancellationToken.None))
            .ReturnsAsync(CreateRenderedImage());
        GetSharedPublicationSocialImageQueryHandler handler = new GetSharedPublicationSocialImageQueryHandler(
            new Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>>(MockBehavior.Strict).Object,
            yearHandler.Object,
            new Mock<IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>>>(MockBehavior.Strict).Object,
            renderer.Object);

        ApplicationResult<ShareSocialImageRenderResult> result = await handler.HandleAsync(
            new GetSharedPublicationSocialImageQuery(
                "year-token",
                SharePublicationType.YearRecap,
                9,
                "fr"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        yearHandler.VerifyAll();
        renderer.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldExcludePassportIdentityFieldsThatWereNotPublished()
    {
        SharedPassportProfileResult profile = new SharedPassportProfileResult(
            DateTime.UnixEpoch,
            new PassportProfileSharePreviewResult(
                null,
                null,
                null,
                ShareVisibility.Unlisted,
                false,
                2,
                4,
                null,
                null,
                null,
                null,
                Array.Empty<PassportProfileShareCountryResult>(),
                Array.Empty<PassportProfileShareYearResult>(),
                new[]
                {
                    new PassportProfileShareParkResult(
                        "Phantasialand",
                        "DE",
                        2,
                        2024,
                        2026,
                        null,
                        null),
                },
                Array.Empty<PassportProfileShareRatingResult>(),
                Array.Empty<PassportProfileShareMissedItemResult>(),
                false,
                "passport-v1",
                false),
            11);
        Mock<IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>>> passportHandler =
            new Mock<IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>>>(MockBehavior.Strict);
        passportHandler.Setup(value => value.HandleAsync(
                It.Is<GetSharedPassportProfileQuery>(query => query.ShareId == "passport-token"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharedPassportProfileResult>.Success(profile));
        Mock<IShareSocialImageRenderer> renderer = new Mock<IShareSocialImageRenderer>(MockBehavior.Strict);
        renderer.Setup(value => value.RenderAsync(
                It.Is<ShareSocialImageModel>(model =>
                    model.PublicationType == SharePublicationType.PassportProfile
                    && model.Subject == null
                    && model.Metrics.Count == 2
                    && model.Metrics.All(metric => metric.Kind != ShareSocialImageMetricKind.Rides)
                    && model.Highlight == "Phantasialand"
                    && model.PublicationVersion == 11),
                CancellationToken.None))
            .ReturnsAsync(CreateRenderedImage());
        GetSharedPublicationSocialImageQueryHandler handler = new GetSharedPublicationSocialImageQueryHandler(
            new Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>>(MockBehavior.Strict).Object,
            new Mock<IQueryHandler<GetSharedYearRecapQuery, ApplicationResult<SharedYearRecapResult>>>(MockBehavior.Strict).Object,
            passportHandler.Object,
            renderer.Object);

        ApplicationResult<ShareSocialImageRenderResult> result = await handler.HandleAsync(
            new GetSharedPublicationSocialImageQuery(
                "passport-token",
                SharePublicationType.PassportProfile,
                11,
                "en"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        passportHandler.VerifyAll();
        renderer.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenUrlTargetsAnOlderPublicationVersion_ShouldNotRenderAnything()
    {
        SharedVisitRecapResult recap = new SharedVisitRecapResult(
            DateTime.UnixEpoch,
            new VisitRecapSharePreviewResult(
                "park-1",
                "Demo Park",
                null,
                null,
                null,
                Array.Empty<string>(),
                null,
                null,
                null,
                Array.Empty<VisitRecapShareItemResult>(),
                null,
                true,
                false,
                false),
            8);
        Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>> visitHandler =
            new Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>>(MockBehavior.Strict);
        visitHandler.Setup(value => value.HandleAsync(
                It.IsAny<GetSharedVisitRecapQuery>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharedVisitRecapResult>.Success(recap));
        Mock<IShareSocialImageRenderer> renderer = new Mock<IShareSocialImageRenderer>(MockBehavior.Strict);
        GetSharedPublicationSocialImageQueryHandler handler = CreateHandler(
            visitHandler,
            renderer);

        ApplicationResult<ShareSocialImageRenderResult> result = await handler.HandleAsync(
            new GetSharedPublicationSocialImageQuery(
                "opaque-token",
                SharePublicationType.VisitRecap,
                7,
                "fr"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.social-image-not-found");
        renderer.Verify(value => value.RenderAsync(
            It.IsAny<ShareSocialImageModel>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenLanguageIsNotPubliclyServed_ShouldRejectBeforeReadingTheSnapshot()
    {
        Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>> visitHandler =
            new Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>>(MockBehavior.Strict);
        Mock<IShareSocialImageRenderer> renderer = new Mock<IShareSocialImageRenderer>(MockBehavior.Strict);
        GetSharedPublicationSocialImageQueryHandler handler = CreateHandler(
            visitHandler,
            renderer);

        ApplicationResult<ShareSocialImageRenderResult> result = await handler.HandleAsync(
            new GetSharedPublicationSocialImageQuery(
                "opaque-token",
                SharePublicationType.VisitRecap,
                7,
                "ru"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.social-image-language-invalid");
        visitHandler.Verify(value => value.HandleAsync(
            It.IsAny<GetSharedVisitRecapQuery>(),
            It.IsAny<CancellationToken>()), Times.Never);
        renderer.Verify(value => value.RenderAsync(
            It.IsAny<ShareSocialImageModel>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static GetSharedPublicationSocialImageQueryHandler CreateHandler(
        Mock<IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>> visitHandler,
        Mock<IShareSocialImageRenderer> renderer)
    {
        Mock<IQueryHandler<GetSharedYearRecapQuery, ApplicationResult<SharedYearRecapResult>>> yearHandler =
            new Mock<IQueryHandler<GetSharedYearRecapQuery, ApplicationResult<SharedYearRecapResult>>>(MockBehavior.Strict);
        Mock<IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>>> passportHandler =
            new Mock<IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>>>(MockBehavior.Strict);
        return new GetSharedPublicationSocialImageQueryHandler(
            visitHandler.Object,
            yearHandler.Object,
            passportHandler.Object,
            renderer.Object);
    }

    private static ShareSocialImageRenderResult CreateRenderedImage()
    {
        return new ShareSocialImageRenderResult(
            new byte[] { 1, 2, 3 },
            "image/png",
            "texte alternatif",
            "\"etag\"");
    }
}
