using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Services;

public sealed class LiveOperationalWriteCoordinatorTests
{
    [Fact]
    public async Task RunAsync_WhenSameSourceBoundaryIsBusy_ShouldSerializeOperations()
    {
        LiveOperationalWriteCoordinator coordinator = new LiveOperationalWriteCoordinator();
        LiveDataSourceId sourceId = LiveDataSourceId.Parse("source");
        TaskCompletionSource firstEntered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int> first = coordinator.RunAsync(
            sourceId,
            "external-park",
            async _ =>
            {
                firstEntered.SetResult();
                await releaseFirst.Task;
                return 1;
            },
            CancellationToken.None);
        await firstEntered.Task;

        Task<int> second = coordinator.RunAsync(
            sourceId,
            "external-park",
            _ => Task.FromResult(2),
            CancellationToken.None);

        Assert.False(second.IsCompleted);
        releaseFirst.SetResult();
        Assert.Equal(1, await first);
        Assert.Equal(2, await second);
    }
}
