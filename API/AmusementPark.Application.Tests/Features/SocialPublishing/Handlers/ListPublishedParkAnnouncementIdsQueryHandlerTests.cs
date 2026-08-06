using AmusementPark.Application.Features.SocialPublishing.Handlers;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialPublishing.Handlers;

public sealed class ListPublishedParkAnnouncementIdsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldUseFilteredRepositoryProjection()
    {
        Mock<ISocialPublicationRepository> repository = new Mock<ISocialPublicationRepository>(MockBehavior.Strict);
        repository
            .Setup(candidate => candidate.ListPublishedAutomaticParkAnnouncementParkIdsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "park-1", "park-2" });
        ListPublishedParkAnnouncementIdsQueryHandler handler =
            new ListPublishedParkAnnouncementIdsQueryHandler(repository.Object);

        IReadOnlyCollection<string> result = await handler.HandleAsync(
            new ListPublishedParkAnnouncementIdsQuery(),
            CancellationToken.None);

        Assert.Equal(new[] { "park-1", "park-2" }, result);
        repository.VerifyAll();
    }
}
