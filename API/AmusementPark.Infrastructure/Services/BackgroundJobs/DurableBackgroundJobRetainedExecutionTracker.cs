namespace AmusementPark.Infrastructure.Services.BackgroundJobs;

internal sealed class DurableBackgroundJobRetainedExecutionTracker
{
    private readonly object gate = new object();
    private readonly HashSet<Task> executions = new HashSet<Task>();

    public void Track(Task execution)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if (execution.IsCompleted)
        {
            return;
        }

        lock (this.gate)
        {
            this.executions.Add(execution);
        }

        _ = this.RemoveWhenCompletedAsync(execution);
    }

    public async Task WaitForAllAsync()
    {
        while (true)
        {
            Task[] snapshot;
            lock (this.gate)
            {
                snapshot = this.executions.ToArray();
            }

            if (snapshot.Length == 0)
            {
                return;
            }

            try
            {
                await Task.WhenAll(snapshot);
            }
            catch
            {
                // The execution owner records failures; shutdown still waits for every retained lifetime to end.
            }
        }
    }

    private async Task RemoveWhenCompletedAsync(Task execution)
    {
        try
        {
            await execution;
        }
        catch
        {
            // The execution owner records failures.
        }
        finally
        {
            lock (this.gate)
            {
                this.executions.Remove(execution);
            }
        }
    }
}
