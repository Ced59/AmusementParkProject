using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.Identifiers;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class WatchlistAccountDeletionServiceTests
{
    [Fact]
    public async Task DeleteAsync_NormalizesIdentityAndReturnsCompleteReceipt()
    {
        WatchlistAccountDeletionResult receipt = new WatchlistAccountDeletionResult(
            2,
            3,
            5,
            7,
            1,
            4,
            6);
        Mock<IWatchlistAccountDeletionStore> store =
            new Mock<IWatchlistAccountDeletionStore>(MockBehavior.Strict);
        store.Setup(candidate => candidate.PurgeAsync("user-1", CancellationToken.None))
            .ReturnsAsync(receipt);
        WatchlistAccountDeletionService service = new WatchlistAccountDeletionService(store.Object);

        WatchlistAccountDeletionResult result = await service.DeleteAsync(
            "  user-1  ",
            CancellationToken.None);

        Assert.Same(receipt, result);
        Assert.Equal(28, result.PurgedDocumentCount);
        store.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_RejectsMissingIdentityBeforeAccessingStore()
    {
        Mock<IWatchlistAccountDeletionStore> store =
            new Mock<IWatchlistAccountDeletionStore>(MockBehavior.Strict);
        WatchlistAccountDeletionService service = new WatchlistAccountDeletionService(store.Object);

        await Assert.ThrowsAsync<IdentifierValidationException>(() => service.DeleteAsync(
            " ",
            CancellationToken.None));

        store.VerifyNoOtherCalls();
    }
}
