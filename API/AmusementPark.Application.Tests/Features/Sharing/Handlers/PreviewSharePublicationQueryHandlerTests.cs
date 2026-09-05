using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

public sealed class PreviewSharePublicationQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenBuilderExists_ShouldValidatePolicyAndDelegate()
    {
        SharePublicationPreviewResult preview = new SharePublicationPreviewResult(
            SharePublicationType.PersonalRanking,
            4,
            1,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings },
            new PersonalRankingSharePreviewResult(null, null, null, Array.Empty<PersonalRankingSharePreviewItemResult>(), false));
        Mock<ISharePublicationPreviewBuilder> builder =
            new Mock<ISharePublicationPreviewBuilder>(MockBehavior.Strict);
        builder.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.PersonalRanking);
        builder.Setup(value => value.BuildAsync(
                "owner-1",
                null,
                It.Is<ShareContentPolicy>(policy =>
                    policy.PublicationType == SharePublicationType.PersonalRanking
                    && policy.DatePrecision == ShareDatePrecision.Hidden
                    && policy.Includes(ShareContentField.GlobalRatings)),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharePublicationPreviewResult>.Success(preview));
        PreviewSharePublicationQueryHandler handler =
            new PreviewSharePublicationQueryHandler(new[] { builder.Object });

        ApplicationResult<SharePublicationPreviewResult> result = await handler.HandleAsync(
            new PreviewSharePublicationQuery(
                " owner-1 ",
                SharePublicationType.PersonalRanking,
                null,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.GlobalRatings }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(preview, result.Value);
        builder.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPolicyRequestsForbiddenField_ShouldRejectBeforeReadingSource()
    {
        Mock<ISharePublicationPreviewBuilder> builder =
            new Mock<ISharePublicationPreviewBuilder>(MockBehavior.Strict);
        builder.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.PersonalRanking);
        PreviewSharePublicationQueryHandler handler =
            new PreviewSharePublicationQueryHandler(new[] { builder.Object });

        ApplicationResult<SharePublicationPreviewResult> result = await handler.HandleAsync(
            new PreviewSharePublicationQuery(
                "owner-1",
                SharePublicationType.PersonalRanking,
                null,
                ShareDatePrecision.Day,
                Array.Empty<ShareContentField>()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == ShareContentPolicyErrorCodes.DatePrecisionNotAllowed);
        builder.Verify(value => value.BuildAsync(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<ShareContentPolicy>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenTypeHasNoBuilder_ShouldReturnAControlledRuleViolation()
    {
        PreviewSharePublicationQueryHandler handler =
            new PreviewSharePublicationQueryHandler(Array.Empty<ISharePublicationPreviewBuilder>());

        ApplicationResult<SharePublicationPreviewResult> result = await handler.HandleAsync(
            new PreviewSharePublicationQuery(
                "owner-1",
                SharePublicationType.VisitRecap,
                "visit-1",
                ShareDatePrecision.Hidden,
                Array.Empty<ShareContentField>()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.preview-type-not-available");
    }
}
