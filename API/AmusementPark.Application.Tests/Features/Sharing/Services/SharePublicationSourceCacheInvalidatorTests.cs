using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class SharePublicationSourceCacheInvalidatorTests
{
    private const string OwnerId = "owner-1";
    private const string VisitId = "visit-1";
    private const string ShareTokenValue = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime Now =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task InvalidateVisitDeletionAsync_ShouldPurgeEveryAffectedPublicRecap()
    {
        IReadOnlyCollection<(SharePublicationType Type, string ScopeKey)> sources =
            new List<(SharePublicationType Type, string ScopeKey)>
            {
                (
                    SharePublicationType.VisitRecap,
                    VisitRecapShareSourceScope.Create(OwnerId, VisitId)),
                (
                    SharePublicationType.PassportProfile,
                    PassportProfileShareSourceScope.Create(OwnerId)),
                (
                    SharePublicationType.PersonalRanking,
                    PersonalRankingShareSourceScope.Create(OwnerId)),
                (
                    SharePublicationType.YearRecap,
                    YearRecapShareSourceScope.Create(OwnerId, 2026)),
            };
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        foreach ((SharePublicationType type, string scopeKey) in sources)
        {
            repository.Setup(value => value.GetOwnedBySourceAsync(
                    OwnerId,
                    type,
                    scopeKey,
                    CancellationToken.None))
                .ReturnsAsync(CreatePublication(type, scopeKey));
        }

        List<SharePublicationCacheInvalidationJobPayload> requests =
            new List<SharePublicationCacheInvalidationJobPayload>();
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.IsAny<EnqueueExactBackgroundJobRequest>(),
                CancellationToken.None))
            .Callback((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
            {
                SharePublicationCacheInvalidationJobPayload payload =
                    request.Payload.Deserialize<SharePublicationCacheInvalidationJobPayload>()
                    ?? throw new InvalidOperationException("Missing payload.");
                requests.Add(payload);
            })
            .ReturnsAsync((DurableBackgroundJob)null!);
        SharePublicationCacheInvalidationScheduler scheduler =
            new SharePublicationCacheInvalidationScheduler(jobs.Object);
        SharePublicationSourceCacheInvalidator invalidator =
            new SharePublicationSourceCacheInvalidator(
                repository.Object,
                scheduler);

        await invalidator.InvalidateVisitDeletionAsync(
            OwnerId,
            VisitId,
            2026,
            CancellationToken.None);

        Assert.Equal(4, requests.Count);
        Assert.Equal(
            sources.Select(static source => source.Type).OrderBy(static type => type),
            requests.Select(static request => request.PublicationType).OrderBy(static type => type));
        Assert.All(requests, request => Assert.Contains(ShareTokenValue, request.ShareIds));
        repository.VerifyAll();
        jobs.VerifyAll();
    }

    private static SharePublication CreatePublication(
        SharePublicationType type,
        string sourceScopeKey)
    {
        ShareContentPolicy policy = ShareContentPolicy.CreatePrivateDefault(type);
        return SharePublication.Restore(
            SharePublicationId.New(),
            OwnerId,
            type,
            sourceScopeKey,
            ShareToken.Parse(ShareTokenValue),
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            policy,
            1,
            1,
            1,
            Now,
            null,
            Now,
            Now);
    }
}
