using System.Diagnostics;
using System.Diagnostics.Metrics;
using AmusementPark.Application.Features.LiveData.Models;

namespace AmusementPark.Infrastructure.Services.LiveData;

internal sealed class LivePollingMetrics : IDisposable
{
    private readonly Meter meter = new Meter("AmusementPark.LivePolling", "1.0.0");
    private readonly Counter<long> executionCounter;
    private readonly Counter<long> circuitOpenedCounter;
    private readonly Histogram<double> durationHistogram;

    public LivePollingMetrics()
    {
        this.executionCounter = this.meter.CreateCounter<long>("live_polling.executions");
        this.circuitOpenedCounter = this.meter.CreateCounter<long>("live_polling.circuits.opened");
        this.durationHistogram = this.meter.CreateHistogram<double>(
            "live_polling.execution.duration",
            "ms");
    }

    public void Record(
        LivePollingTarget target,
        LivePollingExecutionResult result,
        TimeSpan elapsed)
    {
        TagList tags = new TagList
        {
            { "source.id", target.SourceId.Value },
            { "poll.disposition", result.Disposition.ToString() },
        };
        this.executionCounter.Add(1, tags);
        this.durationHistogram.Record(elapsed.TotalMilliseconds, tags);
        if (result.CircuitOpened)
        {
            this.circuitOpenedCounter.Add(1, tags);
        }
    }

    public void Dispose()
    {
        this.meter.Dispose();
    }
}
