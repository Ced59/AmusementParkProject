using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Infrastructure.Services.Sharing;

internal sealed class ShareSocialImageRenderWork
{
    private readonly CancellationTokenSource queueCancellation = new CancellationTokenSource();
    private readonly Lazy<Task<ShareSocialImageRenderResult>> rendering;
    private readonly object waiterLock = new object();
    private int waiterCount;
    private bool abandoned;

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

    public bool TryAttachWaiter()
    {
        lock (this.waiterLock)
        {
            if (this.abandoned)
            {
                return false;
            }

            this.waiterCount++;
            return true;
        }
    }

    public async Task<ShareSocialImageRenderResult> WaitAsync(CancellationToken cancellationToken)
    {
        Task<ShareSocialImageRenderResult> renderingTask = this.rendering.Value;
        try
        {
            return await renderingTask.WaitAsync(cancellationToken);
        }
        finally
        {
            bool cancelQueuedRender = false;
            lock (this.waiterLock)
            {
                this.waiterCount--;
                if (this.waiterCount == 0 && !renderingTask.IsCompleted)
                {
                    this.abandoned = true;
                    cancelQueuedRender = true;
                }
            }

            if (cancelQueuedRender)
            {
                this.queueCancellation.Cancel();
            }
        }
    }
}
