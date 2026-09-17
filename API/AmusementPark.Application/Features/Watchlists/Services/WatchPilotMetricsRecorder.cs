using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Watchlists;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class WatchPilotMetricsRecorder
{
    private readonly IWatchPilotMetricsRepository metricsRepository;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<WatchPilotMetricsRecorder> logger;

    public WatchPilotMetricsRecorder(
        IWatchPilotMetricsRepository metricsRepository,
        ILogger<WatchPilotMetricsRecorder> logger,
        TimeProvider? timeProvider = null)
    {
        this.metricsRepository = metricsRepository ?? throw new ArgumentNullException(nameof(metricsRepository));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task RecordBestEffortAsync(
        WatchPilotInteractionKind interactionKind,
        CancellationToken cancellationToken)
    {
        try
        {
            DateOnly dateUtc = DateOnly.FromDateTime(this.timeProvider.GetUtcNow().UtcDateTime);
            await this.metricsRepository.IncrementInteractionAsync(
                dateUtc,
                interactionKind,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.logger.LogDebug(
                "Watch pilot metric {InteractionKind} was not recorded because the request was cancelled.",
                interactionKind);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Watch pilot metric {InteractionKind} could not be recorded.",
                interactionKind);
        }
    }
}
