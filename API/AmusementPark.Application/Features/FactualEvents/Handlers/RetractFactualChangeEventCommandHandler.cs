using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Commands;
using AmusementPark.Application.Features.FactualEvents.Services;
using AmusementPark.Application.Features.Watchlists.Ports;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.FactualEvents.Handlers;

public sealed class RetractFactualChangeEventCommandHandler
    : ICommandHandler<RetractFactualChangeEventCommand, ApplicationResult>
{
    private readonly FactualChangeEventAdministrationService service;
    private readonly IFactualNotificationDistributionScheduler scheduler;
    private readonly ILogger<RetractFactualChangeEventCommandHandler> logger;

    public RetractFactualChangeEventCommandHandler(
        FactualChangeEventAdministrationService service,
        IFactualNotificationDistributionScheduler scheduler,
        ILogger<RetractFactualChangeEventCommandHandler> logger)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApplicationResult> HandleAsync(
        RetractFactualChangeEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ApplicationResult result = await this.service.RetractAsync(
            command.EventId,
            command.ExpectedVersion,
            command.ReasonCode,
            cancellationToken);
        if (result.IsSuccess)
        {
            await this.TryScheduleAsync(command.EventId, command.ExpectedVersion + 1, cancellationToken);
        }

        return result;
    }

    private async Task TryScheduleAsync(
        string eventId,
        long eventVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            await this.scheduler.ScheduleCorrectionAsync(eventId, eventVersion, null, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "The factual event {FactualEventId} was retracted, but redistribution scheduling failed; reconciliation will retry.",
                eventId);
        }
    }
}
