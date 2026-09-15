using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Commands;
using AmusementPark.Application.Features.FactualEvents.Services;
using AmusementPark.Application.Features.Watchlists.Ports;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.FactualEvents.Handlers;

public sealed class PublishFactualChangeEventCommandHandler
    : ICommandHandler<PublishFactualChangeEventCommand, ApplicationResult>
{
    private readonly FactualChangeEventAdministrationService service;
    private readonly IFactualNotificationDistributionScheduler distributionScheduler;
    private readonly ILogger<PublishFactualChangeEventCommandHandler> logger;

    public PublishFactualChangeEventCommandHandler(
        FactualChangeEventAdministrationService service,
        IFactualNotificationDistributionScheduler distributionScheduler,
        ILogger<PublishFactualChangeEventCommandHandler> logger)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.distributionScheduler = distributionScheduler
            ?? throw new ArgumentNullException(nameof(distributionScheduler));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApplicationResult> HandleAsync(
        PublishFactualChangeEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ApplicationResult result = await this.service.PublishAsync(
            command.EventId,
            command.ExpectedVersion,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result;
        }

        try
        {
            await this.distributionScheduler.ScheduleAsync(
                command.EventId,
                null,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "The factual event {FactualEventId} was published, but its notification job could not be scheduled; reconciliation will retry.",
                command.EventId);
        }

        return result;
    }
}
