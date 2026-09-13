using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Microsoft.Extensions.Logging;
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

        List<SharePublicationCacheInvalidationRequest> requests =
            new List<SharePublicationCacheInvalidationRequest>();
        Mock<ISharePublicationCacheInvalidationQueue> queue =
            new Mock<ISharePublicationCacheInvalidationQueue>(MockBehavior.Strict);
        queue.Setup(value => value.Enqueue(It.IsAny<SharePublicationCacheInvalidationRequest>()))
            .Callback((SharePublicationCacheInvalidationRequest request) => requests.Add(request));
        SharePublicationSourceCacheInvalidator invalidator =
            new SharePublicationSourceCacheInvalidator(
                repository.Object,
                Mock.Of<ILogger<SharePublicationSourceCacheInvalidator>>(),
                queue.Object);

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
        queue.VerifyAll();
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
