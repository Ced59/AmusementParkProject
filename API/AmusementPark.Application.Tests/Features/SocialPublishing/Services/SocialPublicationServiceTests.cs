using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialPublishing.Services;

public sealed class SocialPublicationServiceTests
{
    [Fact]
    public async Task PublishManualAsync_WhenRequestIsValid_ShouldPublishAndPersistResult()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        SocialPublicationService service = CreateService(repository, publisher);

        ApplicationResult<SocialPublication> result = await service.PublishManualAsync(
            new SocialLinkPublicationRequest(
                SocialNetwork.Facebook,
                " Découvrez ce parc ! ",
                "https://amusement-parks.fun/fr/park/park-1/test#gallery"),
            "admin-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(SocialPublicationStatus.Published, result.Value.Status);
        Assert.Equal("Découvrez ce parc !", result.Value.Message);
        Assert.Equal("https://amusement-parks.fun/fr/park/park-1/test", result.Value.Url);
        Assert.Equal("facebook-post-1", result.Value.ExternalPostId);
        Assert.Equal(1, publisher.PublishCallCount);
        Assert.Single(repository.Publications);
    }

    [Fact]
    public async Task PublishManualAsync_WhenUrlTargetsAnotherOrigin_ShouldRejectWithoutPersisting()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        SocialPublicationService service = CreateService(repository, publisher);

        ApplicationResult<SocialPublication> result = await service.PublishManualAsync(
            new SocialLinkPublicationRequest(SocialNetwork.Facebook, "Message", "https://example.org/article"),
            "admin-1",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "social-publishing.url.invalid");
        Assert.Empty(repository.Publications);
        Assert.Equal(0, publisher.PublishCallCount);
    }

    [Fact]
    public async Task PublishParkAnnouncementAsync_WhenParkAlreadyHasPublication_ShouldNotPublishTwice()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        SocialPublicationService service = CreateService(repository, publisher);
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc Étincelle",
            IsVisible = true,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        SocialPublication? first = await service.PublishParkAnnouncementAsync(park, "admin-1", CancellationToken.None);
        SocialPublication? second = await service.PublishParkAnnouncementAsync(park, "admin-1", CancellationToken.None);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal("facebook:park:park-1", first.DeduplicationKey);
        Assert.Contains("Un nouveau parc", first.Message, StringComparison.Ordinal);
        Assert.Contains("A new park", first.Message, StringComparison.Ordinal);
        Assert.Equal("https://amusement-parks.fun/fr/park/park-1/parc-etincelle", first.Url);
        Assert.Equal(1, publisher.PublishCallCount);
        Assert.Single(repository.Publications);
    }

    [Fact]
    public async Task PublishParkAnnouncementAsync_WhenPublisherIsDisabled_ShouldRecordRetryableFailure()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        StubSocialPublisher publisher = StubSocialPublisher.Disabled();
        SocialPublicationService service = CreateService(repository, publisher);
        Park park = new Park
        {
            Id = "park-1",
            Name = "Test Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        SocialPublication? publication = await service.PublishParkAnnouncementAsync(park, null, CancellationToken.None);

        Assert.NotNull(publication);
        Assert.Equal(SocialPublicationStatus.Failed, publication.Status);
        Assert.Equal("publisher-not-configured", publication.FailureCode);
        Assert.Equal(0, publisher.PublishCallCount);
    }

    [Fact]
    public async Task RetryAsync_WhenFailedPublicationExists_ShouldPublishSameRecord()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        SocialPublication failedPublication = new SocialPublication
        {
            Id = "publication-1",
            Network = SocialNetwork.Facebook,
            Status = SocialPublicationStatus.Failed,
            Trigger = SocialPublicationTrigger.Manual,
            Message = "Message",
            Url = "https://amusement-parks.fun/fr/home",
            FailureCode = "old-error",
        };
        repository.Publications.Add(failedPublication);
        SocialPublicationService service = CreateService(repository, publisher);

        ApplicationResult<SocialPublication> result = await service.RetryAsync(
            "publication-1",
            "admin-2",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SocialPublicationStatus.Published, result.Value!.Status);
        Assert.Equal("admin-2", result.Value.RequestedByUserId);
        Assert.Null(result.Value.FailureCode);
        Assert.Equal(1, publisher.PublishCallCount);
        Assert.Single(repository.Publications);
    }

    [Fact]
    public async Task UpdateAsync_WhenPublishedPostExists_ShouldUpdateFacebookAndStoredMessage()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        SocialPublication publication = CreatePublishedPublication();
        repository.Publications.Add(publication);
        SocialPublicationService service = CreateService(repository, publisher);

        ApplicationResult<SocialPublication> result = await service.UpdateAsync(
            publication.Id!,
            " Nouveau texte ",
            "admin-2",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Nouveau texte", result.Value!.Message);
        Assert.Equal("admin-2", result.Value.RequestedByUserId);
        Assert.NotNull(result.Value.LastSynchronizedAtUtc);
        Assert.Equal(1, publisher.UpdateCallCount);
    }

    [Fact]
    public async Task DeleteAsync_WhenPostWasAlreadyRemovedFromFacebook_ShouldMarkItDeleted()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        publisher.DeleteResult = new SocialPublisherOperationResult(false, true, null, null);
        SocialPublication publication = CreatePublishedPublication();
        repository.Publications.Add(publication);
        SocialPublicationService service = CreateService(repository, publisher);

        ApplicationResult<SocialPublication> result = await service.DeleteAsync(
            publication.Id!,
            "admin-2",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SocialPublicationStatus.Deleted, result.Value!.Status);
        Assert.NotNull(result.Value.DeletedAtUtc);
        Assert.Equal(1, publisher.DeleteCallCount);
    }

    [Fact]
    public async Task SynchronizeAsync_WhenTrackedPostNoLongerExists_ShouldMarkItDeleted()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        publisher.SnapshotResult = new SocialPublisherPostSnapshotResult(true, false, null, null, null, null);
        SocialPublication publication = CreatePublishedPublication();
        repository.Publications.Add(publication);
        SocialPublicationService service = CreateService(repository, publisher);

        SocialPublicationSynchronizationResult result = await service.SynchronizeAsync(25, CancellationToken.None);

        Assert.Equal(1, result.CheckedCount);
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(SocialPublicationStatus.Deleted, publication.Status);
        Assert.Equal(1, publisher.GetCallCount);
    }

    private static SocialPublication CreatePublishedPublication()
    {
        return new SocialPublication
        {
            Id = "publication-1",
            Network = SocialNetwork.Facebook,
            Status = SocialPublicationStatus.Published,
            Trigger = SocialPublicationTrigger.Manual,
            Message = "Message",
            Url = "https://amusement-parks.fun/fr/home",
            ExternalPostId = "123_456",
            ExternalPostUrl = "https://www.facebook.com/123/posts/456",
        };
    }

    private static SocialPublicationService CreateService(
        InMemorySocialPublicationRepository repository,
        StubSocialPublisher publisher)
    {
        return new SocialPublicationService(
            repository,
            new[] { publisher },
            new StubPublicSeoContextProvider());
    }

    private sealed class StubPublicSeoContextProvider : IPublicSeoContextProvider
    {
        public Task<PublicSeoContext> GetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new PublicSeoContext(
                "https://amusement-parks.fun",
                new[] { "en", "fr" }));
        }
    }

    private sealed class StubSocialPublisher : ISocialPublisher
    {
        private readonly SocialPublisherDescriptor descriptor;

        private StubSocialPublisher(SocialPublisherDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }

        public SocialNetwork Network => SocialNetwork.Facebook;

        public int PublishCallCount { get; private set; }

        public int UpdateCallCount { get; private set; }

        public int DeleteCallCount { get; private set; }

        public int GetCallCount { get; private set; }

        public SocialPublisherOperationResult DeleteResult { get; set; } =
            new SocialPublisherOperationResult(true, false, null, null);

        public SocialPublisherPostSnapshotResult SnapshotResult { get; set; } =
            new SocialPublisherPostSnapshotResult(
                true,
                true,
                "Message",
                "https://www.facebook.com/123/posts/456",
                null,
                null);

        public static StubSocialPublisher Configured()
        {
            return new StubSocialPublisher(new SocialPublisherDescriptor(
                SocialNetwork.Facebook,
                "Facebook",
                true,
                true,
                "https://www.facebook.com/test",
                true));
        }

        public static StubSocialPublisher Disabled()
        {
            return new StubSocialPublisher(new SocialPublisherDescriptor(
                SocialNetwork.Facebook,
                "Facebook",
                false,
                false,
                null,
                true));
        }

        public SocialPublisherDescriptor Describe()
        {
            return this.descriptor;
        }

        public Task<SocialPublisherResult> PublishLinkAsync(SocialPublisherRequest request, CancellationToken cancellationToken)
        {
            this.PublishCallCount++;
            return Task.FromResult(new SocialPublisherResult(
                true,
                "facebook-post-1",
                "https://www.facebook.com/facebook-post-1",
                null,
                null));
        }

        public Task<SocialPublisherOperationResult> UpdatePostAsync(
            string externalPostId,
            string message,
            CancellationToken cancellationToken)
        {
            this.UpdateCallCount++;
            return Task.FromResult(new SocialPublisherOperationResult(true, false, null, null));
        }

        public Task<SocialPublisherOperationResult> DeletePostAsync(
            string externalPostId,
            CancellationToken cancellationToken)
        {
            this.DeleteCallCount++;
            return Task.FromResult(this.DeleteResult);
        }

        public Task<SocialPublisherPostSnapshotResult> GetPostAsync(
            string externalPostId,
            CancellationToken cancellationToken)
        {
            this.GetCallCount++;
            return Task.FromResult(this.SnapshotResult);
        }
    }

    private sealed class InMemorySocialPublicationRepository : ISocialPublicationRepository
    {
        public List<SocialPublication> Publications { get; } = new List<SocialPublication>();

        public Task<SocialPublication> CreateAsync(SocialPublication publication, CancellationToken cancellationToken)
        {
            this.Publications.Add(publication);
            return Task.FromResult(publication);
        }

        public Task<SocialPublication> UpdateAsync(SocialPublication publication, CancellationToken cancellationToken)
        {
            int index = this.Publications.FindIndex(current => current.Id == publication.Id);
            if (index >= 0)
            {
                this.Publications[index] = publication;
            }

            return Task.FromResult(publication);
        }

        public Task<SocialPublication?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Publications.FirstOrDefault(publication => publication.Id == id));
        }

        public Task<SocialPublication?> GetByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Publications.FirstOrDefault(
                publication => publication.DeduplicationKey == deduplicationKey));
        }

        public Task<SocialPublication?> GetByExternalPostIdAsync(string externalPostId, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Publications.FirstOrDefault(
                publication => publication.ExternalPostId == externalPostId));
        }

        public Task<IReadOnlyCollection<SocialPublication>> ListRecentAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<SocialPublication> publications = this.Publications.Take(limit).ToList();
            return Task.FromResult(publications);
        }
    }
}
