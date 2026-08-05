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

        public Task<IReadOnlyCollection<SocialPublication>> ListRecentAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<SocialPublication> publications = this.Publications.Take(limit).ToList();
            return Task.FromResult(publications);
        }
    }
}
