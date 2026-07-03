using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Contracts;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Services;

public sealed class ParkWeatherRefreshStarter
{
    private readonly IParkWeatherRunRepository runRepository;
    private readonly IParkWeatherRefreshQueue queue;
    private readonly IParkWeatherRefreshSettings settings;

    public ParkWeatherRefreshStarter(
        IParkWeatherRunRepository runRepository,
        IParkWeatherRefreshQueue queue,
        IParkWeatherRefreshSettings settings)
    {
        this.runRepository = runRepository;
        this.queue = queue;
        this.settings = settings;
    }

    public Task<ApplicationResult<ParkWeatherRunResult>> StartManualFullRefreshAsync(CancellationToken cancellationToken)
    {
        DateOnly nextAutomaticRunLocalDate = this.ResolveNextAutomaticRunLocalDate(DateTime.UtcNow);
        return this.StartAsync(
            ParkWeatherRunTrigger.Manual,
            ParkWeatherRefreshScope.FullVisibleParks,
            sourceRunId: null,
            targetParkId: null,
            cancelsAutomaticRunLocalDate: nextAutomaticRunLocalDate,
            cancellationToken);
    }

    public Task<ApplicationResult<ParkWeatherRunResult>> StartAutomaticRefreshAsync(DateOnly automaticRunLocalDate, CancellationToken cancellationToken)
    {
        return this.StartAsync(
            ParkWeatherRunTrigger.Automatic,
            ParkWeatherRefreshScope.FullVisibleParks,
            sourceRunId: null,
            targetParkId: null,
            cancelsAutomaticRunLocalDate: null,
            cancellationToken);
    }

    public async Task<ApplicationResult<ParkWeatherRunResult>> StartFailedRetryAsync(string sourceRunId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceRunId))
        {
            return ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.InvalidRequest(nameof(sourceRunId), "Run id is required."));
        }

        ParkWeatherRun? sourceRun = await this.runRepository.GetByIdAsync(sourceRunId.Trim(), cancellationToken);
        if (sourceRun is null)
        {
            return ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.RunNotFound());
        }

        IReadOnlyCollection<ParkWeatherRunItem> failedItems = await this.runRepository.GetRunItemsAsync(sourceRun.Id ?? string.Empty, ParkWeatherRunItemStatus.Failed, cancellationToken);
        if (failedItems.Count == 0)
        {
            return ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.NoFailedParkToRetry());
        }

        return await this.StartAsync(
            ParkWeatherRunTrigger.RetryFailed,
            ParkWeatherRefreshScope.FailedFromRun,
            sourceRun.Id,
            targetParkId: null,
            cancelsAutomaticRunLocalDate: null,
            cancellationToken);
    }

    public Task<ApplicationResult<ParkWeatherRunResult>> StartSingleParkRefreshAsync(string parkId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parkId))
        {
            return Task.FromResult(ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.InvalidRequest(nameof(parkId), "Park id is required.")));
        }

        return this.StartAsync(
            ParkWeatherRunTrigger.RetryPark,
            ParkWeatherRefreshScope.SinglePark,
            sourceRunId: null,
            targetParkId: parkId.Trim(),
            cancelsAutomaticRunLocalDate: null,
            cancellationToken);
    }

    private async Task<ApplicationResult<ParkWeatherRunResult>> StartAsync(
        ParkWeatherRunTrigger trigger,
        ParkWeatherRefreshScope scope,
        string? sourceRunId,
        string? targetParkId,
        DateOnly? cancelsAutomaticRunLocalDate,
        CancellationToken cancellationToken)
    {
        bool hasActiveRun = await this.runRepository.HasActiveRunAsync(cancellationToken);
        if (hasActiveRun)
        {
            return ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.ActiveRunExists());
        }

        ParkWeatherRun run = new ParkWeatherRun
        {
            Trigger = trigger,
            Scope = scope,
            Status = ParkWeatherRunStatus.Queued,
            SourceRunId = sourceRunId,
            TargetParkId = targetParkId,
            CancelsAutomaticRunLocalDate = cancelsAutomaticRunLocalDate,
            RequestedAtUtc = DateTime.UtcNow,
            Message = "Weather refresh queued.",
        };

        ParkWeatherRun createdRun = await this.runRepository.CreateAsync(run, cancellationToken);
        try
        {
            await this.queue.EnqueueAsync(new ParkWeatherRefreshJob(createdRun.Id ?? string.Empty), cancellationToken);
            return ApplicationResult<ParkWeatherRunResult>.Success(createdRun.ToResult());
        }
        catch (Exception)
        {
            createdRun.Status = ParkWeatherRunStatus.Failed;
            createdRun.CompletedAtUtc = DateTime.UtcNow;
            createdRun.Message = "Weather refresh could not be queued.";
            await this.runRepository.UpdateAsync(createdRun, CancellationToken.None);
            return ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.QueueUnavailable());
        }
    }

    private DateOnly ResolveNextAutomaticRunLocalDate(DateTime utcNow)
    {
        TimeZoneInfo timeZone = this.ResolveTimeZone();
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone);
        DateTime todayScheduled = localNow.Date
            .AddHours(Math.Clamp(this.settings.AutomaticRefreshHour, 0, 23))
            .AddMinutes(Math.Clamp(this.settings.AutomaticRefreshMinute, 0, 59));

        DateTime nextScheduled = localNow < todayScheduled
            ? todayScheduled
            : todayScheduled.AddDays(1);

        return DateOnly.FromDateTime(nextScheduled);
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(this.settings.AutomaticRefreshTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
