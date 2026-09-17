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
        Mock<IWatchlistAccountDeletionFence> fence =
            new Mock<IWatchlistAccountDeletionFence>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        fence.InSequence(sequence)
            .Setup(candidate => candidate.BlockAsync("user-1", CancellationToken.None))
            .Returns(Task.CompletedTask);
        store.InSequence(sequence)
            .Setup(candidate => candidate.PurgeAsync("user-1", CancellationToken.None))
            .ReturnsAsync(receipt);
        WatchlistAccountDeletionService service = new WatchlistAccountDeletionService(
            fence.Object,
            store.Object);

        WatchlistAccountDeletionResult result = await service.DeleteAsync(
            "  user-1  ",
            CancellationToken.None);

        Assert.Same(receipt, result);
        Assert.Equal(28, result.PurgedDocumentCount);
        store.VerifyAll();
        fence.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_RejectsMissingIdentityBeforeAccessingStore()
    {
        Mock<IWatchlistAccountDeletionStore> store =
            new Mock<IWatchlistAccountDeletionStore>(MockBehavior.Strict);
        Mock<IWatchlistAccountDeletionFence> fence =
            new Mock<IWatchlistAccountDeletionFence>(MockBehavior.Strict);
        WatchlistAccountDeletionService service = new WatchlistAccountDeletionService(
            fence.Object,
            store.Object);

        await Assert.ThrowsAsync<IdentifierValidationException>(() => service.DeleteAsync(
            " ",
            CancellationToken.None));

        store.VerifyNoOtherCalls();
        fence.VerifyNoOtherCalls();
    }
}
