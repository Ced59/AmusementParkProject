using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Weather;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Weather;

public sealed class ParkWeatherAutomaticRefreshBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ParkWeatherSettings settings;
    private readonly ILogger<ParkWeatherAutomaticRefreshBackgroundService> logger;

    public ParkWeatherAutomaticRefreshBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ParkWeatherSettings settings,
        ILogger<ParkWeatherAutomaticRefreshBackgroundService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.settings = settings;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ScheduledWeatherRun nextRun = this.ResolveNextRun(DateTime.UtcNow);
            await Task.Delay(nextRun.Delay, stoppingToken);

            if (!this.settings.IsAutomaticRefreshEnabled)
            {
                continue;
            }

            await this.StartAutomaticRunAsync(nextRun.LocalDate, stoppingToken);
        }
    }

    private async Task StartAutomaticRunAsync(DateOnly localDate, CancellationToken cancellationToken)
    {
        using IServiceScope scope = this.serviceScopeFactory.CreateScope();
        IParkWeatherRunRepository runRepository = scope.ServiceProvider.GetRequiredService<IParkWeatherRunRepository>();

        if (await runRepository.HasAutomaticCancellationAsync(localDate, cancellationToken))
        {
            await runRepository.CreateAsync(new ParkWeatherRun
            {
                Trigger = ParkWeatherRunTrigger.Automatic,
                Scope = ParkWeatherRefreshScope.FullVisibleParks,
                Status = ParkWeatherRunStatus.Skipped,
                RequestedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                Message = "Automatic weather refresh skipped because a manual refresh already covers this cycle.",
            }, cancellationToken);

            this.logger.LogInformation("Automatic weather refresh skipped for local date {LocalDate}.", localDate);
            return;
        }

        ParkWeatherRefreshStarter starter = scope.ServiceProvider.GetRequiredService<ParkWeatherRefreshStarter>();
        await starter.StartAutomaticRefreshAsync(localDate, cancellationToken);
    }

    private ScheduledWeatherRun ResolveNextRun(DateTime utcNow)
    {
        TimeZoneInfo timeZone = this.ResolveTimeZone();
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone);
        DateTime localScheduled = localNow.Date
            .AddHours(Math.Clamp(this.settings.AutomaticRefreshHour, 0, 23))
            .AddMinutes(Math.Clamp(this.settings.AutomaticRefreshMinute, 0, 59));

        if (localScheduled <= localNow)
        {
            localScheduled = localScheduled.AddDays(1);
        }

        DateTime utcScheduled = TimeZoneInfo.ConvertTimeToUtc(localScheduled, timeZone);
        TimeSpan delay = utcScheduled - DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        return new ScheduledWeatherRun(DateOnly.FromDateTime(localScheduled), delay);
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

    private sealed record ScheduledWeatherRun(DateOnly LocalDate, TimeSpan Delay);
}
