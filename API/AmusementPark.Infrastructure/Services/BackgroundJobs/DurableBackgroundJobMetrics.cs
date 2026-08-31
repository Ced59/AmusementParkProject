using System.Diagnostics;
using System.Diagnostics.Metrics;
using AmusementPark.Application.Features.BackgroundJobs.Models;

namespace AmusementPark.Infrastructure.Services.BackgroundJobs;

internal sealed class DurableBackgroundJobMetrics : IDisposable
{
    private readonly Meter meter = new Meter("AmusementPark.BackgroundJobs", "1.0.0");
    private readonly Counter<long> executionCounter;
    private readonly Histogram<double> durationHistogram;
    private readonly Counter<long> recoveredLeaseCounter;

    public DurableBackgroundJobMetrics()
    {
        this.executionCounter = this.meter.CreateCounter<long>("background_jobs.executions");
        this.durationHistogram = this.meter.CreateHistogram<double>(
            "background_jobs.execution.duration",
            "ms");
        this.recoveredLeaseCounter = this.meter.CreateCounter<long>("background_jobs.leases.recovered");
    }

    public void RecordExecution(
        DurableBackgroundJob job,
        DurableBackgroundJobExecutionResult result,
        TimeSpan elapsed)
    {
        TagList tags = new TagList
        {
            { "job.kind", job.Kind },
            { "job.disposition", result.Disposition.ToString() },
        };
        this.executionCounter.Add(1, tags);
        this.durationHistogram.Record(elapsed.TotalMilliseconds, tags);
    }

    public void RecordRecoveredLeases(int recoveredCount)
    {
        if (recoveredCount > 0)
        {
            this.recoveredLeaseCounter.Add(recoveredCount);
        }
    }

    public void Dispose()
    {
        this.meter.Dispose();
    }
}
