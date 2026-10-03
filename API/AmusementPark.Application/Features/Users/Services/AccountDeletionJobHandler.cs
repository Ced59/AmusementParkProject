using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Users.Models;
using AmusementPark.Application.Features.Users.Ports;

namespace AmusementPark.Application.Features.Users.Services;

public sealed class AccountDeletionJobHandler : IDurableBackgroundJobHandler
{
    private const int MaximumAttempts = 12;
    private readonly AccountDeletionCoordinator coordinator;
    private readonly IAccountDeletionOperationRepository operationRepository;

    public AccountDeletionJobHandler(
        AccountDeletionCoordinator coordinator,
        IAccountDeletionOperationRepository operationRepository)
    {
        this.coordinator = coordinator;
        this.operationRepository = operationRepository;
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            AccountDeletionJob.Kind,
            DurableBackgroundJobWorkload.Heavy,
            new[] { AccountDeletionJob.PayloadVersion },
            TimeSpan.FromMinutes(15),
            MaximumAttempts,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromHours(2),
            maximumConcurrency: 1);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        AccountDeletionJobPayload? payload = Deserialize(context);
        if (payload is null || string.IsNullOrWhiteSpace(payload.OperationId))
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                "account-deletion.invalid-payload");
        }

        try
        {
            AccountDeletionOperation? operation =
                await this.operationRepository.GetAsync(
                    payload.OperationId.Trim(),
                    cancellationToken);
            if (operation is null)
            {
                return DurableBackgroundJobHandlerResult.Success();
            }

            await this.coordinator.DeleteAsync(operation.UserId, cancellationToken);
            await this.operationRepository.DeleteAsync(operation.Id, cancellationToken);
            return DurableBackgroundJobHandlerResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return DurableBackgroundJobHandlerResult.Retry(
                "account-deletion.retry-required");
        }
    }

    private static AccountDeletionJobPayload? Deserialize(
        DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != AccountDeletionJob.PayloadVersion)
        {
            return null;
        }

        try
        {
            return context.Payload.Deserialize<AccountDeletionJobPayload>();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
