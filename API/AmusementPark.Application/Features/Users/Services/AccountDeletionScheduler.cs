using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Users.Models;

namespace AmusementPark.Application.Features.Users.Services;

public sealed class AccountDeletionScheduler
{
    private readonly IDurableBackgroundJobRepository jobRepository;

    public AccountDeletionScheduler(IDurableBackgroundJobRepository jobRepository)
    {
        this.jobRepository = jobRepository;
    }

    public Task<DurableBackgroundJob> ScheduleAsync(
        string operationId,
        CancellationToken cancellationToken)
    {
        AccountDeletionJobPayload payload = new AccountDeletionJobPayload(operationId);
        return this.jobRepository.EnqueueExactAsync(
            new EnqueueExactBackgroundJobRequest(
                AccountDeletionJob.Kind,
                $"account-deletion:{operationId}",
                AccountDeletionJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload)),
            cancellationToken);
    }
}
