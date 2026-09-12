using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Models;
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
    private const string RotatedTokenValue = "ISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0-P0A";
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
                    publication.Status == SharePublicationStatus.Draft
                    && !publication.IsResolvable
                    && publication.SourceVersion == 12
                    && publication.ContentPolicy.Includes(ShareContentField.GlobalRatings)
                    && !publication.ContentPolicy.Includes(ShareContentField.PublicDisplayName)
                    && !publication.ContentPolicy.Includes(ShareContentField.Avatar)),
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(publication =>
                    publication.IsResolvable
                    && publication.SourceVersion == 12
                    && publication.ContentPolicy.Includes(ShareContentField.GlobalRatings)),
                0,
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
            repository.Object,
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
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(13);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(repository.Object, tokenFactory.Object),
            repository.Object,
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
        repository.VerifyAll();
        tokenFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenSourceChangesDuringWrite_ShouldNotReportSuccess()
    {
        SharePublication? preparedPublication = null;
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        repository.Setup(value => value.CreateAsync(
                It.IsAny<SharePublication>(),
                CancellationToken.None))
            .Callback((SharePublication publication, CancellationToken _) =>
                preparedPublication = publication)
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(12, 13);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(
                repository.Object,
                tokenFactory.Object,
                new SharePublicationFixedTimeProvider(Now)),
            repository.Object,
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
        Assert.Equal(SharePublicationStatus.Draft, preparedPublication!.Status);
        Assert.False(preparedPublication.IsResolvable);
        repository.VerifyAll();
        tokenFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenFinalSourceReadFails_ShouldLeavePreparedPublicationPrivate()
    {
        SharePublication? preparedPublication = null;
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        repository.Setup(value => value.CreateAsync(
                It.IsAny<SharePublication>(),
                CancellationToken.None))
            .Callback((SharePublication publication, CancellationToken _) =>
                preparedPublication = publication)
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(
            12,
            finalReadFails: true);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(
                repository.Object,
                tokenFactory.Object,
                new SharePublicationFixedTimeProvider(Now)),
            repository.Object,
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
        Assert.Contains(result.Errors, error =>
            error.Code == "share-publication.source-version-unavailable");
        Assert.Equal(SharePublicationStatus.Draft, preparedPublication!.Status);
        Assert.False(preparedPublication.IsResolvable);
        repository.VerifyAll();
        tokenFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenFinalSourceIsUnstable_ShouldNotReportSuccess()
    {
        SharePublication? preparedPublication = null;
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync(() => preparedPublication);
        repository.Setup(value => value.CreateAsync(
                It.IsAny<SharePublication>(),
                CancellationToken.None))
            .Callback((SharePublication publication, CancellationToken _) =>
                preparedPublication = publication)
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(
            12,
            finalReadUnstable: true);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(
                repository.Object,
                tokenFactory.Object,
                new SharePublicationFixedTimeProvider(Now)),
            repository.Object,
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
        Assert.Contains(result.Errors, error =>
            error.Code == SharingApplicationErrors.SourceChangedCode);
        Assert.Equal(SharePublicationStatus.Draft, preparedPublication!.Status);
        Assert.False(preparedPublication.IsResolvable);
        repository.VerifyAll();
        tokenFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenMutationBeginsDuringPublicWrite_ShouldRevokeAndFail()
    {
        SharePublication? storedPublication = null;
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync(() => storedPublication is null
                ? null
                : ClonePublication(storedPublication));
        repository.Setup(value => value.CreateAsync(
                It.IsAny<SharePublication>(),
                CancellationToken.None))
            .Callback((SharePublication publication, CancellationToken _) =>
                storedPublication = ClonePublication(publication))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        repository.Setup(value => value.ReplaceAsync(
                It.IsAny<SharePublication>(),
                It.IsAny<long>(),
                CancellationToken.None))
            .Callback((SharePublication publication, long _, CancellationToken _) =>
                storedPublication = ClonePublication(publication))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        tokenFactory.Setup(value => value.Generate()).Returns(ShareToken.Parse(TokenValue));
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(
            12,
            postPublishReadUnstable: true);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(
                repository.Object,
                tokenFactory.Object,
                new SharePublicationFixedTimeProvider(Now)),
            repository.Object,
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
        Assert.Contains(result.Errors, error =>
            error.Code == SharingApplicationErrors.SourceChangedCode);
        Assert.Equal(SharePublicationStatus.Revoked, storedPublication!.Status);
        Assert.False(storedPublication.IsResolvable);
        repository.Verify(value => value.ReplaceAsync(
            It.IsAny<SharePublication>(),
            It.IsAny<long>(),
            CancellationToken.None), Times.Exactly(2));
        repository.VerifyAll();
        tokenFactory.VerifyAll();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenPostWriteRevokeCannotBeConfirmed_ShouldPreserveSourceFailure()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        SharePublication? storedPublication = CreateNeedsReviewPublication(policy);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync(() => storedPublication is null
                ? null
                : ClonePublication(storedPublication));
        repository.Setup(value => value.GetOwnedAsync(
                storedPublication!.Id,
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync(() => ClonePublication(storedPublication!));
        repository.Setup(value => value.ReplaceAsync(
                It.IsAny<SharePublication>(),
                It.IsAny<long>(),
                CancellationToken.None))
            .ReturnsAsync((SharePublication publication, long _, CancellationToken _) =>
            {
                if (publication.Status != SharePublicationStatus.Published)
                {
                    return SharePublicationWriteOutcome.Conflict;
                }

                storedPublication = ClonePublication(publication);
                return SharePublicationWriteOutcome.Success;
            });
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        tokenFactory.Setup(value => value.Generate()).Returns(ShareToken.Parse(RotatedTokenValue));
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(
            12,
            postPublishReadUnstable: true);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(
                repository.Object,
                tokenFactory.Object,
                new SharePublicationFixedTimeProvider(Now)),
            repository.Object,
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
        Assert.Contains(result.Errors, error =>
            error.Code == SharingApplicationErrors.SourceChangedCode);
        Assert.Equal(SharePublicationStatus.Published, storedPublication!.Status);
        Assert.Equal(RotatedTokenValue, storedPublication.ShareToken?.Value);
        Assert.NotEqual(TokenValue, storedPublication.ShareToken?.Value);
        repository.Verify(value => value.ReplaceAsync(
            It.IsAny<SharePublication>(),
            It.IsAny<long>(),
            CancellationToken.None), Times.Exactly(6));
        repository.VerifyAll();
        tokenFactory.VerifyAll();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenExistingPublicationIsUnchanged_ShouldNotRevokeIt()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        SharePublication existing = SharePublication.Create(
            SharePublicationId.Parse("publication-1"),
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            policy,
            12,
            Now);
        existing.Publish(
            ShareToken.Parse(TokenValue),
            ShareVisibility.Unlisted,
            12,
            policy,
            0,
            Now);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync(existing);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(
            12,
            finalReadUnstable: true);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(
                repository.Object,
                tokenFactory.Object,
                new SharePublicationFixedTimeProvider(Now)),
            repository.Object,
            CreateApprovalProtector(isValid: true));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new PublishSharePublicationCommand(
                OwnerId,
                SharePublicationType.PersonalRanking,
                null,
                12,
                policy.SchemaVersion,
                policy.DatePrecision,
                policy.IncludedFields,
                ApprovalToken),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SharePublicationStatus.Published, existing.Status);
        repository.VerifyAll();
        tokenFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenPublicationChangesAfterApprovalValidation_ShouldExpire()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        SharePublication concurrentPublication = SharePublication.Create(
            SharePublicationId.Parse("publication-2"),
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            policy,
            12,
            Now);
        concurrentPublication.Publish(
            ShareToken.Parse(TokenValue),
            ShareVisibility.Unlisted,
            12,
            policy,
            0,
            Now);
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.SetupSequence(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null)
            .ReturnsAsync(concurrentPublication);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { CreateSourceDescriptor(12) },
            new SharePublicationPublisher(repository.Object, tokenFactory.Object),
            repository.Object,
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
        repository.VerifyAll();
        tokenFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishApprovedPreview_WhenPublicationIsRevokedAfterPreparation_ShouldExpire()
    {
        ShareContentPolicy storedPolicy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        ShareContentPolicy approvedPolicy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.GlobalRatings,
            });
        SharePublication approvalPublication = CreateNeedsReviewPublication(storedPolicy);
        SharePublication preparationPublication = CreateNeedsReviewPublication(storedPolicy);
        SharePublication concurrentlyRevokedPublication = CreateNeedsReviewPublication(storedPolicy);
        concurrentlyRevokedPublication.ReplaceContentPolicy(
            approvedPolicy,
            concurrentlyRevokedPublication.PublicationVersion,
            Now);
        concurrentlyRevokedPublication.Revoke(
            concurrentlyRevokedPublication.PublicationVersion,
            Now);

        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.SetupSequence(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync(approvalPublication)
            .ReturnsAsync(CreateNeedsReviewPublication(storedPolicy))
            .ReturnsAsync(concurrentlyRevokedPublication);
        repository.SetupSequence(value => value.GetOwnedAsync(
                SharePublicationId.Parse("publication-concurrent-preparation"),
                OwnerId,
                CancellationToken.None))
            .ReturnsAsync(preparationPublication)
            .ReturnsAsync(concurrentlyRevokedPublication);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(publication =>
                    publication.Status == SharePublicationStatus.NeedsReview
                    && publication.ContentPolicy.HasSameSelectionAs(approvedPolicy)),
                2,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory =
            new Mock<IShareTokenFactory>(MockBehavior.Strict);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { CreateSourceDescriptor(12) },
            new SharePublicationPublisher(
                repository.Object,
                tokenFactory.Object,
                new SharePublicationFixedTimeProvider(Now)),
            repository.Object,
            CreateApprovalProtector(isValid: true));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new PublishSharePublicationCommand(
                OwnerId,
                SharePublicationType.PersonalRanking,
                null,
                12,
                approvedPolicy.SchemaVersion,
                approvedPolicy.DatePrecision,
                approvedPolicy.IncludedFields,
                ApprovalToken),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Code == "share-publication.preview-expired");
        repository.VerifyAll();
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
            repository.Object,
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
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        Mock<IShareTokenFactory> tokenFactory =
            new Mock<IShareTokenFactory>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(12);
        PublishSharePublicationCommandHandler handler = new PublishSharePublicationCommandHandler(
            new[] { source },
            new SharePublicationPublisher(repository.Object, tokenFactory.Object),
            repository.Object,
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
        repository.VerifyAll();
        tokenFactory.VerifyNoOtherCalls();
    }

    private static ISharePublicationSourceDescriptor CreateSourceDescriptor(
        long sourceVersion,
        long? persistedSourceVersion = null,
        bool finalReadFails = false,
        bool finalReadUnstable = false,
        bool postPublishReadUnstable = false)
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
        if (postPublishReadUnstable)
        {
            source.SetupSequence(value => value.GetCurrentSourceVersionAsync(
                    It.Is<SharePublicationSourceVersionRequest>(request =>
                        request.SourceScopeKey == ScopeKey),
                    CancellationToken.None))
                .ReturnsAsync(ApplicationResult<long>.Success(sourceVersion))
                .ReturnsAsync(ApplicationResult<long>.Success(sourceVersion))
                .ReturnsAsync(ApplicationResult<long>.Failure(
                    SharingApplicationErrors.SourceChangedDuringPreview()));
        }
        else if (finalReadFails)
        {
            source.SetupSequence(value => value.GetCurrentSourceVersionAsync(
                    It.Is<SharePublicationSourceVersionRequest>(request =>
                        request.SourceScopeKey == ScopeKey),
                    CancellationToken.None))
                .ReturnsAsync(ApplicationResult<long>.Success(sourceVersion))
                .ReturnsAsync(ApplicationResult<long>.Failure(
                    SharingApplicationErrors.SourceVersionUnavailable()));
        }
        else if (finalReadUnstable)
        {
            source.SetupSequence(value => value.GetCurrentSourceVersionAsync(
                    It.Is<SharePublicationSourceVersionRequest>(request =>
                        request.SourceScopeKey == ScopeKey),
                    CancellationToken.None))
                .ReturnsAsync(ApplicationResult<long>.Success(sourceVersion))
                .ReturnsAsync(ApplicationResult<long>.Failure(
                    SharingApplicationErrors.SourceChangedDuringPreview()));
        }
        else if (persistedSourceVersion.HasValue)
        {
            source.SetupSequence(value => value.GetCurrentSourceVersionAsync(
                    It.Is<SharePublicationSourceVersionRequest>(request =>
                        request.SourceScopeKey == ScopeKey),
                    CancellationToken.None))
                .ReturnsAsync(ApplicationResult<long>.Success(sourceVersion))
                .ReturnsAsync(ApplicationResult<long>.Success(persistedSourceVersion.Value));
        }
        else
        {
            source.Setup(value => value.GetCurrentSourceVersionAsync(
                    It.Is<SharePublicationSourceVersionRequest>(request =>
                        request.SourceScopeKey == ScopeKey),
                    CancellationToken.None))
                .ReturnsAsync(ApplicationResult<long>.Success(sourceVersion));
        }

        return source.Object;
    }

    private static SharePublication CreateNeedsReviewPublication(ShareContentPolicy policy)
    {
        SharePublication publication = SharePublication.Create(
            SharePublicationId.Parse("publication-concurrent-preparation"),
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            policy,
            11,
            Now);
        publication.Publish(
            ShareToken.Parse(TokenValue),
            ShareVisibility.Unlisted,
            11,
            policy,
            0,
            Now);
        publication.MarkSourceChanged(12, Now);
        return publication;
    }

    private static SharePublication ClonePublication(SharePublication source)
    {
        return SharePublication.Restore(
            source.Id,
            source.OwnerUserId,
            source.Type,
            source.SourceScopeKey,
            source.ShareToken,
            source.Status,
            source.Visibility,
            source.ContentPolicy,
            source.SourceVersion,
            source.PublicationVersion,
            source.Version,
            source.PublishedAtUtc,
            source.RevokedAtUtc,
            source.CreatedAtUtc,
            source.UpdatedAtUtc);
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
                It.IsAny<SharePublicationApprovalState>(),
                It.IsAny<ShareContentPolicy>()))
            .Returns(isValid);
        return approvalProtector.Object;
    }
}
