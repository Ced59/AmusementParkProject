using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Services;

public sealed class FactualChangeEventAdministrationService
{
    private readonly IFactualChangeEventRepository repository;
    private readonly TimeProvider timeProvider;

    public FactualChangeEventAdministrationService(
        IFactualChangeEventRepository repository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<ApplicationResult> VerifyAsync(
        string eventId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(
            eventId,
            expectedVersion,
            static (factualEvent, timestamp) => factualEvent.Verify(timestamp),
            cancellationToken);
    }

    public Task<ApplicationResult> PublishAsync(
        string eventId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(
            eventId,
            expectedVersion,
            static (factualEvent, timestamp) => factualEvent.Publish(timestamp),
            cancellationToken);
    }

    private async Task<ApplicationResult> ChangeStatusAsync(
        string eventId,
        long expectedVersion,
        Action<FactualChangeEvent, DateTime> transition,
        CancellationToken cancellationToken)
    {
        if (!FactualChangeEventId.TryParse(eventId, out FactualChangeEventId parsedEventId)
            || expectedVersion < 1)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.InvalidMutation());
        }

        FactualChangeEvent? factualEvent = await this.repository.GetAsync(
            parsedEventId,
            cancellationToken);
        if (factualEvent is null)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.NotFound());
        }

        if (factualEvent.Version != expectedVersion)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.Conflict());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime mutationAtUtc = nowUtc < factualEvent.UpdatedAtUtc
            ? factualEvent.UpdatedAtUtc
            : nowUtc;
        try
        {
            transition(factualEvent, mutationAtUtc);
        }
        catch (FactualEventValidationException)
        {
            return ApplicationResult.Failure(FactualEventAdministrationErrors.InvalidTransition());
        }

        FactualChangeEventMutationOutcome outcome = await this.repository.ReplaceAsync(
            factualEvent,
            expectedVersion,
            cancellationToken);
        return outcome == FactualChangeEventMutationOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(FactualEventAdministrationErrors.Conflict());
    }
}
