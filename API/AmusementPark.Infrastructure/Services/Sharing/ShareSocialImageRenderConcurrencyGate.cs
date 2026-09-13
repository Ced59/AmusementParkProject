namespace AmusementPark.Infrastructure.Services.Sharing;

internal sealed class ShareSocialImageRenderConcurrencyGate
{
    private const int MaximumConcurrentRenders = 2;
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(
        MaximumConcurrentRenders,
        MaximumConcurrentRenders);

    public async Task<TValue> RunAsync<TValue>(Func<Task<TValue>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await this.semaphore.WaitAsync(CancellationToken.None);
        try
        {
            return await operation();
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}
