using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class SharePublicationPublisherCacheInvalidationTests
{
    private const string PreviousToken = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private const string NextToken = "ISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0-P0A";
    private static readonly DateTime Now =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PublishAsync_WhenRepublishing_ShouldInvalidatePreviousAndCurrentLinks()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        SharePublication publication = SharePublication.Create(
            SharePublicationId.Parse("publication-republish"),
            "owner-1",
            SharePublicationType.PersonalRanking,
            PersonalRankingShareSourceScope.Create("owner-1"),
            policy,
            11,
            Now.AddDays(-2));
        publication.Publish(
            ShareToken.Parse(PreviousToken),
            ShareVisibility.Unlisted,
            11,
            policy,
            0,
            Now.AddDays(-2));
        publication.MarkSourceChanged(12, Now.AddDays(-1));

        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                "owner-1",
                SharePublicationType.PersonalRanking,
                publication.SourceScopeKey,
                CancellationToken.None))
            .ReturnsAsync(publication);
        repository.Setup(value => value.GetOwnedAsync(
                publication.Id,
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(publication);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.IsResolvable
                    && candidate.ShareToken == ShareToken.Parse(NextToken)
                    && candidate.PublicationVersion == 3),
                2,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        tokenFactory.Setup(value => value.Generate()).Returns(ShareToken.Parse(NextToken));
        Mock<ISharePublicationSourceDescriptor> source =
            new Mock<ISharePublicationSourceDescriptor>(MockBehavior.Strict);
        source.Setup(value => value.GetCurrentSourceVersionAsync(
                It.Is<SharePublicationSourceVersionRequest>(request =>
                    request.SourceScopeKey == publication.SourceScopeKey),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<long>.Success(12));
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    ContainsExpectedLinks(request, publication.Id.Value)),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        SharePublicationPublisher publisher = new SharePublicationPublisher(
            repository.Object,
            tokenFactory.Object,
            new SharePublicationFixedTimeProvider(Now),
            invalidationScheduler: new SharePublicationCacheInvalidationScheduler(jobs.Object));

        ApplicationResult<SharePublicationSettingsResult> result = await publisher.PublishAsync(
            "owner-1",
            SharePublicationType.PersonalRanking,
            publication.SourceScopeKey,
            12,
            SharePublicationApprovalState.From(publication),
            policy,
            source.Object,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NextToken, result.Value!.ShareId);
        jobs.VerifyAll();
        repository.VerifyAll();
        tokenFactory.VerifyAll();
        source.Verify(value => value.GetCurrentSourceVersionAsync(
            It.IsAny<SharePublicationSourceVersionRequest>(),
            CancellationToken.None), Times.Exactly(2));
    }

    private static bool ContainsExpectedLinks(
        EnqueueExactBackgroundJobRequest request,
        string publicationId)
    {
        SharePublicationCacheInvalidationJobPayload? payload =
            request.Payload.Deserialize<SharePublicationCacheInvalidationJobPayload>();
        return payload is not null
            && payload.PublicationId == publicationId
            && payload.MinimumPublicationStateVersion == 3
            && payload.ShareIds.Contains(PreviousToken)
            && payload.ShareIds.Contains(NextToken);
    }
}
