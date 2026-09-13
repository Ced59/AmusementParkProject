using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;

namespace AmusementPark.Infrastructure.Services.BackgroundJobs;

internal sealed class DurableBackgroundJobClaim : IDisposable
{
    private Action? release;

    public DurableBackgroundJobClaim(DurableBackgroundJob job, Action release)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(release);
        this.Job = job;
        this.release = release;
    }

    public DurableBackgroundJob Job { get; }

    public async Task ReleaseAfterAsync(Task completion, IDisposable dependencyScope)
    {
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(dependencyScope);
        try
        {
            await completion;
        }
        finally
        {
            this.Dispose();
            dependencyScope.Dispose();
        }
    }

    public void Dispose()
    {
        Action? releaseAction = Interlocked.Exchange(ref this.release, null);
        releaseAction?.Invoke();
    }
}
