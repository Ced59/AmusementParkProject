using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class RequestPassportExportCommandHandler
    : ICommandHandler<RequestPassportExportCommand, ApplicationResult<PassportExport>>
{
    private readonly IPassportExportRepository exportRepository;
    private readonly PassportExportScheduler scheduler;
    private readonly IPassportClock clock;
    private readonly ILogger<RequestPassportExportCommandHandler> logger;

    public RequestPassportExportCommandHandler(
        IPassportExportRepository exportRepository,
        PassportExportScheduler scheduler,
        IPassportClock clock,
        ILogger<RequestPassportExportCommandHandler> logger)
    {
        this.exportRepository = exportRepository;
        this.scheduler = scheduler;
        this.clock = clock;
        this.logger = logger;
    }

    public async Task<ApplicationResult<PassportExport>> HandleAsync(
        RequestPassportExportCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<PassportExport>.Failure(
                ApplicationErrors.Required(nameof(command.UserId)));
        }

        if (!Enum.IsDefined(command.Format))
        {
            return ApplicationResult<PassportExport>.Failure(
                PassportApplicationErrors.InvalidExportFormat());
        }

        DateTime nowUtc = this.clock.UtcNow;
        PassportExport passportExport = new PassportExport(
            Guid.NewGuid().ToString("N"),
            command.UserId.Trim(),
            command.Format,
            PassportExportStatus.Pending,
            CanonicalVisitExportWriter.SchemaVersion,
            nowUtc,
            nowUtc,
            nowUtc.Add(PassportExportJob.Retention));
        await this.exportRepository.CreateAsync(passportExport, cancellationToken);

        try
        {
            _ = await this.scheduler.ScheduleAsync(passportExport, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Passport export {ExportId} will be scheduled by reconciliation.",
                passportExport.Id);
        }

        return ApplicationResult<PassportExport>.Success(passportExport);
    }
}
