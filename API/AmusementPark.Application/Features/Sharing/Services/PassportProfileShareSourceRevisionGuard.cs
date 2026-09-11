using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PassportProfileShareSourceRevisionGuard
    : IPassportProfileShareSourceRevisionGuard
{
    private readonly IShareSourceRevisionRepository sourceRevisionRepository;
    private readonly ILogger<PassportProfileShareSourceRevisionGuard> logger;

    public PassportProfileShareSourceRevisionGuard(
        IShareSourceRevisionRepository sourceRevisionRepository,
        ILogger<PassportProfileShareSourceRevisionGuard> logger)
    {
        this.sourceRevisionRepository = sourceRevisionRepository
            ?? throw new ArgumentNullException(nameof(sourceRevisionRepository));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ShareSourceMutationLease?> TryBeginMutationAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        return await this.sourceRevisionRepository.TryBeginMutationAsync(
            PassportProfileShareSourceScope.Create(ownerUserId),
            cancellationToken);
    }

    public async Task CompleteMutationAsync(
        ShareSourceMutationLease? mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken)
    {
        if (mutationLease is null)
        {
            return;
        }

        try
        {
            await this.sourceRevisionRepository.CompleteMutationAsync(
                mutationLease,
                sourceChanged,
                cancellationToken);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Unable to settle the passport profile source mutation for {ScopeKey}; its lease will expire conservatively.",
                mutationLease.ScopeKey);
        }
    }
}
