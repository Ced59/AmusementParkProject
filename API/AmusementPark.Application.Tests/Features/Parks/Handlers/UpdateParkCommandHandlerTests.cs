using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class UpdateParkCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenStatusIsOmitted_ShouldPreserveExistingStatus()
    {
        Park existingPark = CreatePark(ParkStatus.ClosedDefinitively);
        Park requestedPark = CreatePark(ParkStatus.Operating);
        Park? savedPark = null;
        UpdateParkCommandHandler handler = CreateHandler(existingPark, (park) => savedPark = park);

        ApplicationResult<Park> result = await handler.HandleAsync(
            new UpdateParkCommand("park-1", requestedPark, PreserveExistingStatus: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedPark);
        Assert.Equal(ParkStatus.ClosedDefinitively, savedPark!.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenStatusIsExplicit_ShouldUseRequestedStatus()
    {
        Park existingPark = CreatePark(ParkStatus.ClosedDefinitively);
        Park requestedPark = CreatePark(ParkStatus.Operating);
        Park? savedPark = null;
        UpdateParkCommandHandler handler = CreateHandler(existingPark, (park) => savedPark = park);

        ApplicationResult<Park> result = await handler.HandleAsync(
            new UpdateParkCommand("park-1", requestedPark),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedPark);
        Assert.Equal(ParkStatus.Operating, savedPark!.Status);
    }

    private static UpdateParkCommandHandler CreateHandler(Park existingPark, Action<Park> captureSavedPark)
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPark);
        parkRepository
            .Setup(repository => repository.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .Callback<string, Park, CancellationToken>((_, park, _) => captureSavedPark(park))
            .ReturnsAsync((string _, Park park, CancellationToken _) => park);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(notifier => notifier.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new UpdateParkCommandHandler(
            parkRepository.Object,
            parkItemRepository.Object,
            searchProjectionWriter.Object,
            publicSeoUpdateNotifier.Object);
    }

    private static Park CreatePark(ParkStatus status)
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Demo Park",
            Status = status,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        park.SetPosition(48.85, 2.35);
        return park;
    }
}
