using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class UserCollectionLifecycleServiceTests
{
    [Fact]
    public async Task AddAsync_ReturnsExistingEntry_WhenIntentAlreadyExists()
    {
        UserCollectionEntry existing = UserCollectionEntry.Create(
            UserCollectionEntryId.Parse("entry-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            UserCollectionKind.Favorite,
            CollectionTargetStatus.Available,
            null,
            null,
            null,
            DateTime.UtcNow.AddMinutes(-1));
        Mock<IUserCollectionEntryRepository> collectionRepository = new(MockBehavior.Strict);
        collectionRepository.Setup(repository => repository.GetOwnedByIdentityAsync(
                "user-1",
                CollectionTargetType.Park,
                "park-1",
                UserCollectionKind.Favorite,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        UserCollectionLifecycleService service = CreateService(
            collectionRepository,
            CreatePublicPark("park-1", "Europa-Park", ParkStatus.Operating));

        AmusementPark.Application.Errors.ApplicationResult<UserCollectionEntryResult> result =
            await service.AddAsync(
                "user-1",
                new UserCollectionTargetInput(
                    CollectionTargetType.Park,
                    "park-1",
                    UserCollectionKind.Favorite),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("entry-1", result.Value?.EntryId);
        Assert.Equal("Europa-Park", result.Value?.TargetName);
        collectionRepository.Verify(
            repository => repository.CreateAsync(
                It.IsAny<UserCollectionEntry>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_RejectsHiddenTarget()
    {
        Mock<IUserCollectionEntryRepository> collectionRepository = new(MockBehavior.Strict);
        collectionRepository.Setup(repository => repository.GetOwnedByIdentityAsync(
                "user-1",
                CollectionTargetType.Park,
                "park-hidden",
                UserCollectionKind.WantToVisit,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserCollectionEntry?)null);
        Park hiddenPark = CreatePublicPark("park-hidden", "Hidden", ParkStatus.Operating);
        hiddenPark.IsVisible = false;
        UserCollectionLifecycleService service = CreateService(collectionRepository, hiddenPark);

        AmusementPark.Application.Errors.ApplicationResult<UserCollectionEntryResult> result =
            await service.AddAsync(
                "user-1",
                new UserCollectionTargetInput(
                    CollectionTargetType.Park,
                    "park-hidden",
                    UserCollectionKind.WantToVisit),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "collection.target-not-found");
    }

    [Fact]
    public async Task DeleteAsync_IsIdempotent()
    {
        Mock<IUserCollectionEntryRepository> collectionRepository = new(MockBehavior.Strict);
        collectionRepository.Setup(repository => repository.DeleteOwnedByIdentityAsync(
                "user-1",
                CollectionTargetType.ParkItem,
                "item-1",
                UserCollectionKind.Favorite,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        UserCollectionLifecycleService service = CreateService(collectionRepository, null);

        AmusementPark.Application.Errors.ApplicationResult result = await service.DeleteAsync(
            "user-1",
            new UserCollectionTargetInput(
                CollectionTargetType.ParkItem,
                "item-1",
                UserCollectionKind.Favorite),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ListAsync_PersistsCurrentStatusBeforeUsingItAsFallback()
    {
        UserCollectionEntry entry = UserCollectionEntry.Create(
            UserCollectionEntryId.Parse("entry-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            UserCollectionKind.Favorite,
            CollectionTargetStatus.Available,
            null,
            null,
            null,
            DateTime.UtcNow.AddMinutes(-1));
        Mock<IUserCollectionEntryRepository> collectionRepository = new(MockBehavior.Strict);
        collectionRepository.Setup(repository => repository.ListOwnedAsync(
                "user-1",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { entry });
        collectionRepository.Setup(repository => repository.SynchronizeTargetStatusesAsync(
                It.Is<IReadOnlyCollection<UserCollectionEntry>>(entries =>
                    entries.Count == 1
                    && entries.Single().TargetStatus == CollectionTargetStatus.PermanentlyClosed),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        UserCollectionLifecycleService service = CreateService(
            collectionRepository,
            CreatePublicPark("park-1", "Closed park", ParkStatus.ClosedDefinitively));

        AmusementPark.Application.Errors.ApplicationResult<
            IReadOnlyCollection<UserCollectionEntryResult>> result = await service.ListAsync(
                "user-1",
                null,
                null,
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CollectionTargetStatus.PermanentlyClosed, result.Value?.Single().TargetStatus);
        Assert.Equal(2, result.Value?.Single().Version);
        collectionRepository.VerifyAll();
    }

    private static UserCollectionLifecycleService CreateService(
        Mock<IUserCollectionEntryRepository> collectionRepository,
        Park? park)
    {
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new(MockBehavior.Strict);
        if (park is not null)
        {
            parkRepository.Setup(repository => repository.GetByIdsAsync(
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { park });
            imageRepository.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                    ImageOwnerType.Park,
                    It.IsAny<IReadOnlyCollection<string>>(),
                    ImageCategory.Park,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, string>());
        }

        UserCollectionTargetReader reader = new(
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);
        return new UserCollectionLifecycleService(collectionRepository.Object, reader);
    }

    private static Park CreatePublicPark(string id, string name, ParkStatus status)
    {
        return new Park
        {
            Id = id,
            Name = name,
            IsVisible = true,
            Status = status,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }
}
