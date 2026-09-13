using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Infrastructure.Services.Sharing;

internal sealed class ShareSocialImageRenderWork
{
    private readonly CancellationTokenSource queueCancellation = new CancellationTokenSource();
    private readonly Lazy<Task<ShareSocialImageRenderResult>> rendering;
    private int waiterCount;

    public ShareSocialImageRenderWork(
        ShareSocialImageRenderConcurrencyGate concurrencyGate,
        Func<Task<ShareSocialImageRenderResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(concurrencyGate);
        ArgumentNullException.ThrowIfNull(operation);
        this.rendering = new Lazy<Task<ShareSocialImageRenderResult>>(
            () => concurrencyGate.RunAsync(operation, this.queueCancellation.Token),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public bool IsCanceledOrFaulted
    {
        get
        {
            return this.rendering.IsValueCreated
                && (this.rendering.Value.IsCanceled || this.rendering.Value.IsFaulted);
        }
    }

    public async Task<ShareSocialImageRenderResult> WaitAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref this.waiterCount);
        Task<ShareSocialImageRenderResult> renderingTask = this.rendering.Value;
        try
        {
            return await renderingTask.WaitAsync(cancellationToken);
        }
        finally
        {
            if (Interlocked.Decrement(ref this.waiterCount) == 0 && !renderingTask.IsCompleted)
            {
                this.queueCancellation.Cancel();
            }
        }
    }
}
