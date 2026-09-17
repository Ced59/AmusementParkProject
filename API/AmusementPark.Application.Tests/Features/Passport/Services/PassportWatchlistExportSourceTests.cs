using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class PassportWatchlistExportSourceTests
{
    [Fact]
    public async Task LoadAsync_ResolvesReadableTargetsWithoutExposingStoredIdentifiers()
    {
        UserCollectionEntry entry = UserCollectionEntry.Create(
            UserCollectionEntryId.Parse("entry-internal"),
            "user-1",
            CollectionTargetType.Park,
            "park-internal",
            UserCollectionKind.WantToVisit,
            CollectionTargetStatus.Available,
            "À découvrir avec les enfants",
            1,
            null,
            DateTime.UtcNow);
        PassportWatchlistStoredExportData stored = PassportWatchlistStoredExportData.Empty with
        {
            CollectionEntries = new[] { entry },
        };
        Mock<IWatchlistExportStore> store = new Mock<IWatchlistExportStore>(MockBehavior.Strict);
        store.Setup(candidate => candidate.LoadAsync(
                "user-1",
                It.IsAny<PassportExportSourceBudget>(),
                CancellationToken.None))
            .ReturnsAsync(stored);
        Park park = new Park
        {
            Id = "park-internal",
            Name = "Europa-Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-internal" })),
                CancellationToken.None))
            .ReturnsAsync(new[] { park });
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.Park,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-internal" })),
                ImageCategory.Park,
                true,
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string>());
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        events.Setup(repository => repository.GetManyAsync(
                It.Is<IReadOnlyCollection<FactualChangeEventId>>(ids => ids.Count == 0),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<FactualChangeEvent>());
        PassportWatchlistExportSource source = new PassportWatchlistExportSource(
            store.Object,
            events.Object,
            new UserCollectionTargetReader(parks.Object, parkItems.Object, images.Object));

        PassportWatchlistExportData result = await source.LoadAsync(
            "user-1",
            new PassportExportSourceBudget(1_024),
            CancellationToken.None);

        PassportWatchlistTargetSnapshot target = Assert.Single(result.ParkTargets).Value;
        Assert.Equal("Europa-Park", target.Name);
        Assert.Equal("À découvrir avec les enfants", Assert.Single(result.CollectionEntries).PrivateNote);
        store.VerifyAll();
        parks.VerifyAll();
        images.VerifyAll();
        events.VerifyAll();
        parkItems.VerifyNoOtherCalls();
    }
}
