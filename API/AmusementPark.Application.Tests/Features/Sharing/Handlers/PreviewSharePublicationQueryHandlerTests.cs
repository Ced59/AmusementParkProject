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
        Mock<ISharePublicationSourceDescriptor> source =
            new Mock<ISharePublicationSourceDescriptor>(MockBehavior.Strict);
        source.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.PersonalRanking);
        source.Setup(value => value.ResolveSourceScopeKey("owner-1", null))
            .Returns(ApplicationResult<string>.Success("personal-ranking:owner-1"));
        Mock<ISharePublicationPreviewApprovalProtector> approvalProtector =
            new Mock<ISharePublicationPreviewApprovalProtector>(MockBehavior.Strict);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                "owner-1",
                SharePublicationType.PersonalRanking,
                "personal-ranking:owner-1",
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        approvalProtector.Setup(value => value.CreateToken(
                "owner-1",
                SharePublicationType.PersonalRanking,
                "personal-ranking:owner-1",
                4,
                It.Is<SharePublicationApprovalState>(state =>
                    state.PublicationId == null
                    && state.PersistenceVersion == null),
                It.Is<ShareContentPolicy>(policy =>
                    policy.Includes(ShareContentField.GlobalRatings))))
            .Returns("approved-preview");
        PreviewSharePublicationQueryHandler handler =
            new PreviewSharePublicationQueryHandler(
                new[] { builder.Object },
                new[] { source.Object },
                repository.Object,
                approvalProtector.Object);

        ApplicationResult<SharePublicationPreviewResult> result = await handler.HandleAsync(
            new PreviewSharePublicationQuery(
                " owner-1 ",
                SharePublicationType.PersonalRanking,
                null,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.GlobalRatings }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("approved-preview", result.Value!.ApprovalToken);
        builder.VerifyAll();
        source.VerifyAll();
        repository.Verify(value => value.GetOwnedBySourceAsync(
            "owner-1",
            SharePublicationType.PersonalRanking,
            "personal-ranking:owner-1",
            CancellationToken.None), Times.Exactly(2));
        approvalProtector.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPolicyRequestsForbiddenField_ShouldRejectBeforeReadingSource()
    {
        Mock<ISharePublicationPreviewBuilder> builder =
            new Mock<ISharePublicationPreviewBuilder>(MockBehavior.Strict);
        builder.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.PersonalRanking);
        PreviewSharePublicationQueryHandler handler =
            new PreviewSharePublicationQueryHandler(
                new[] { builder.Object },
                Array.Empty<ISharePublicationSourceDescriptor>(),
                Mock.Of<ISharePublicationRepository>(MockBehavior.Strict),
                Mock.Of<ISharePublicationPreviewApprovalProtector>(MockBehavior.Strict));

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
    public async Task HandleAsync_WhenPublicationChangesDuringPreview_ShouldDiscardApproval()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        SharePublicationPreviewResult preview = new SharePublicationPreviewResult(
            SharePublicationType.PersonalRanking,
            4,
            policy.SchemaVersion,
            policy.DatePrecision,
            policy.IncludedFields,
            new PersonalRankingSharePreviewResult(
                null,
                null,
                null,
                Array.Empty<PersonalRankingSharePreviewItemResult>(),
                false));
        Mock<ISharePublicationPreviewBuilder> builder =
            new Mock<ISharePublicationPreviewBuilder>(MockBehavior.Strict);
        builder.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.PersonalRanking);
        builder.Setup(value => value.BuildAsync(
                "owner-1",
                null,
                It.IsAny<ShareContentPolicy>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharePublicationPreviewResult>.Success(preview));
        Mock<ISharePublicationSourceDescriptor> source =
            new Mock<ISharePublicationSourceDescriptor>(MockBehavior.Strict);
        source.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.PersonalRanking);
        source.Setup(value => value.ResolveSourceScopeKey("owner-1", null))
            .Returns(ApplicationResult<string>.Success("personal-ranking:owner-1"));
        SharePublication changedPublication = SharePublication.Create(
            SharePublicationId.Parse("publication-1"),
            "owner-1",
            SharePublicationType.PersonalRanking,
            "personal-ranking:owner-1",
            policy,
            4,
            new DateTime(2026, 9, 7, 1, 0, 0, DateTimeKind.Utc));
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.SetupSequence(value => value.GetOwnedBySourceAsync(
                "owner-1",
                SharePublicationType.PersonalRanking,
                "personal-ranking:owner-1",
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null)
            .ReturnsAsync(changedPublication);
        Mock<ISharePublicationPreviewApprovalProtector> approvalProtector =
            new Mock<ISharePublicationPreviewApprovalProtector>(MockBehavior.Strict);
        PreviewSharePublicationQueryHandler handler = new PreviewSharePublicationQueryHandler(
            new[] { builder.Object },
            new[] { source.Object },
            repository.Object,
            approvalProtector.Object);

        ApplicationResult<SharePublicationPreviewResult> result = await handler.HandleAsync(
            new PreviewSharePublicationQuery(
                "owner-1",
                SharePublicationType.PersonalRanking,
                null,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.GlobalRatings }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Code == "share-publication.concurrent-modification");
        builder.VerifyAll();
        source.VerifyAll();
        repository.VerifyAll();
        approvalProtector.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenTypeHasNoBuilder_ShouldReturnAControlledRuleViolation()
    {
        PreviewSharePublicationQueryHandler handler =
            new PreviewSharePublicationQueryHandler(
                Array.Empty<ISharePublicationPreviewBuilder>(),
                Array.Empty<ISharePublicationSourceDescriptor>(),
                Mock.Of<ISharePublicationRepository>(MockBehavior.Strict),
                Mock.Of<ISharePublicationPreviewApprovalProtector>(MockBehavior.Strict));

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
