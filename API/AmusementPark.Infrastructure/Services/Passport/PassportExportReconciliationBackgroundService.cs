using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Passport;

public sealed class PassportExportReconciliationBackgroundService : BackgroundService
{
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    internal static readonly TimeSpan MinimumPendingAge = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan MaximumProcessingAge = TimeSpan.FromMinutes(12);
    private const int BatchSize = 20;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<PassportExportReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;

    public PassportExportReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PassportExportReconciliationBackgroundService> logger,
        TimeProvider? timeProvider = null)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Passport export reconciliation failed.");
            }

            await Task.Delay(PollInterval, this.timeProvider, stoppingToken);
        }
    }

    internal async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = this.scopeFactory.CreateScope();
        IPassportExportRepository repository =
            scope.ServiceProvider.GetRequiredService<IPassportExportRepository>();
        PassportExportScheduler scheduler =
            scope.ServiceProvider.GetRequiredService<PassportExportScheduler>();
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        _ = await repository.FailStaleProcessingAsync(
            nowUtc.Subtract(MaximumProcessingAge),
            nowUtc,
            PassportExportErrorCodes.TimedOut,
            nowUtc,
            BatchSize,
            cancellationToken);
        IReadOnlyCollection<PassportExport> exports =
            await repository.ListPendingForReconciliationAsync(
                nowUtc.Subtract(MinimumPendingAge),
                nowUtc,
                BatchSize,
                cancellationToken);
        foreach (PassportExport passportExport in exports)
        {
            _ = await scheduler.ScheduleAsync(passportExport, cancellationToken);
        }
    }
}
