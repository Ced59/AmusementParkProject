using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

public sealed class PublishSharePublicationCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const string ScopeKey = "personal-ranking:owner-1";
    private const string ApprovalToken = "approved-preview";
    private const string TokenValue = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime Now = new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PublishApprovedPreview_ShouldPersistTheExactAnonymousSelection()
    {
        Mock<ISharePublicationRepository> repository = new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        repository.Setup(value => value.CreateAsync(
                It.Is<SharePublication>(publication =>
                    publication.IsResolvable
                    && publication.SourceVersion == 12
                    && publication.ContentPolicy.Includes(ShareContentField.GlobalRatings)
                    && !publication.ContentPolicy.Includes(ShareContentField.PublicDisplayName)
                    && !publication.ContentPolicy.Includes(ShareContentField.Avatar)),
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        tokenFactory.Setup(value => value.Generate()).Returns(ShareToken.Parse(TokenValue));
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(12);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(
                repository.Object,
                tokenFactory.Object,
                new SharePublicationFixedTimeProvider(Now)),
            CreateApprovalProtector(isValid: true));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new PublishSharePublicationCommand(
                OwnerId,
                SharePublicationType.PersonalRanking,
                null,
                12,
                ShareContentPolicy.CurrentSchemaVersion,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.GlobalRatings },
                ApprovalToken),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsPublic);
        Assert.Equal(new[] { ShareContentField.GlobalRatings }, result.Value.IncludedFields);
        repository.VerifyAll();
        tokenFactory.VerifyAll();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenSourceChanged_ShouldRequireANewPreview()
    {
        Mock<ISharePublicationRepository> repository = new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(13);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(repository.Object, tokenFactory.Object),
            CreateApprovalProtector(isValid: true));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new PublishSharePublicationCommand(
                OwnerId,
                SharePublicationType.PersonalRanking,
                null,
                12,
                ShareContentPolicy.CurrentSchemaVersion,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.GlobalRatings },
                ApprovalToken),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "share-publication.preview-expired");
        repository.VerifyNoOtherCalls();
        tokenFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishApprovedPreview_WithoutRankingContent_ShouldRejectThePolicy()
    {
        Mock<ISharePublicationRepository> repository = new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(12);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(repository.Object, tokenFactory.Object),
            Mock.Of<ISharePublicationPreviewApprovalProtector>(MockBehavior.Strict));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new PublishSharePublicationCommand(
                OwnerId,
                SharePublicationType.PersonalRanking,
                null,
                12,
                ShareContentPolicy.CurrentSchemaVersion,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.PublicDisplayName },
                ApprovalToken),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "share-publication.required-content-missing");
        repository.VerifyNoOtherCalls();
        tokenFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenSelectionDiffersFromApproval_ShouldExposeNothing()
    {
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        Mock<IShareTokenFactory> tokenFactory =
            new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(12);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(repository.Object, tokenFactory.Object),
            CreateApprovalProtector(isValid: false));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new PublishSharePublicationCommand(
                OwnerId,
                SharePublicationType.PersonalRanking,
                null,
                12,
                ShareContentPolicy.CurrentSchemaVersion,
                ShareDatePrecision.Hidden,
                new[]
                {
                    ShareContentField.PublicDisplayName,
                    ShareContentField.GlobalRatings,
                },
                ApprovalToken),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Code == "share-publication.preview-approval-invalid");
        repository.VerifyNoOtherCalls();
        tokenFactory.VerifyNoOtherCalls();
    }

    private static ISharePublicationSourceDescriptor CreateSourceDescriptor(long sourceVersion)
    {
        PersonalRankingSharePublicationSource realSource = new PersonalRankingSharePublicationSource(
            Mock.Of<IShareSourceRevisionRepository>());
        Mock<ISharePublicationSourceDescriptor> source =
            new Mock<ISharePublicationSourceDescriptor>(MockBehavior.Strict);
        source.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.PersonalRanking);
        source.Setup(value => value.ResolveSourceScopeKey(OwnerId, null))
            .Returns(ApplicationResult<string>.Success(ScopeKey));
        source.Setup(value => value.ValidatePolicyForPublication(It.IsAny<ShareContentPolicy>()))
            .Returns((ShareContentPolicy policy) => realSource.ValidatePolicyForPublication(policy));
        source.Setup(value => value.GetCurrentSourceVersionAsync(ScopeKey, CancellationToken.None))
            .ReturnsAsync(ApplicationResult<long>.Success(sourceVersion));
        return source.Object;
    }

    private static ISharePublicationPreviewApprovalProtector CreateApprovalProtector(bool isValid)
    {
        Mock<ISharePublicationPreviewApprovalProtector> approvalProtector =
            new Mock<ISharePublicationPreviewApprovalProtector>(MockBehavior.Strict);
        approvalProtector.Setup(value => value.IsValid(
                ApprovalToken,
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                12,
                It.IsAny<ShareContentPolicy>()))
            .Returns(isValid);
        return approvalProtector.Object;
    }
}
