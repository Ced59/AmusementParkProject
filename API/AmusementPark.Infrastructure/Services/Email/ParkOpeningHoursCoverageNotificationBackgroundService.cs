using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Infrastructure.Configuration.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Email;

public sealed class ParkOpeningHoursCoverageNotificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly EmailNotificationSettings settings;
    private readonly ILogger<ParkOpeningHoursCoverageNotificationBackgroundService> logger;

    public ParkOpeningHoursCoverageNotificationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        EmailNotificationSettings settings,
        ILogger<ParkOpeningHoursCoverageNotificationBackgroundService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.settings = settings;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ScheduledOpeningHoursNotificationRun nextRun = this.ResolveNextRun(DateTime.UtcNow);
            await Task.Delay(nextRun.Delay, stoppingToken);

            if (!this.settings.OpeningHoursCoverageNotificationsEnabled || string.IsNullOrWhiteSpace(this.settings.AdminAddress))
            {
                continue;
            }

            await this.ProcessAsync(stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = this.serviceScopeFactory.CreateScope();
            ParkOpeningHoursCoverageNotificationProcessor processor = scope.ServiceProvider.GetRequiredService<ParkOpeningHoursCoverageNotificationProcessor>();
            await processor.ProcessAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Opening hours coverage notification processing failed.");
        }
    }

    private ScheduledOpeningHoursNotificationRun ResolveNextRun(DateTime utcNow)
    {
        TimeZoneInfo timeZone = this.ResolveTimeZone();
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone);
        DateTime localScheduled = localNow.Date
            .AddHours(Math.Clamp(this.settings.OpeningHoursCoverageNotificationHour, 0, 23))
            .AddMinutes(Math.Clamp(this.settings.OpeningHoursCoverageNotificationMinute, 0, 59));

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

        return new ScheduledOpeningHoursNotificationRun(delay);
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        string configuredTimeZoneId = this.settings.OpeningHoursCoverageNotificationTimeZoneId;
        if (!string.IsNullOrWhiteSpace(configuredTimeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(configuredTimeZoneId.Trim(), out TimeZoneInfo? directTimeZone))
        {
            return directTimeZone;
        }

        if (!string.IsNullOrWhiteSpace(configuredTimeZoneId)
            && TimeZoneInfo.TryConvertIanaIdToWindowsId(configuredTimeZoneId.Trim(), out string? windowsTimeZoneId)
            && !string.IsNullOrWhiteSpace(windowsTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsTimeZoneId, out TimeZoneInfo? windowsTimeZone))
        {
            return windowsTimeZone;
        }

        if (!string.IsNullOrWhiteSpace(configuredTimeZoneId)
            && TimeZoneInfo.TryConvertWindowsIdToIanaId(configuredTimeZoneId.Trim(), out string? ianaTimeZoneId)
            && !string.IsNullOrWhiteSpace(ianaTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(ianaTimeZoneId, out TimeZoneInfo? ianaTimeZone))
        {
            return ianaTimeZone;
        }

        return TimeZoneInfo.Utc;
    }

    private sealed record ScheduledOpeningHoursNotificationRun(TimeSpan Delay);
}
