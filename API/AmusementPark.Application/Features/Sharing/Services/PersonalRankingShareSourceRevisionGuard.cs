using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PersonalRankingShareSourceRevisionGuard
    : IPersonalRankingShareSourceRevisionGuard
{
    private readonly IShareSourceRevisionRepository sourceRevisionRepository;
    private readonly ILogger<PersonalRankingShareSourceRevisionGuard> logger;

    public PersonalRankingShareSourceRevisionGuard(
        IShareSourceRevisionRepository sourceRevisionRepository,
        ILogger<PersonalRankingShareSourceRevisionGuard> logger)
    {
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.logger = logger;
    }

    public async Task<ShareSourceMutationLease?> BeginIdentityMutationAsync(
        string ownerUserId,
        PersonalRankingShareIdentityState before,
        PersonalRankingShareIdentityState after,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        return before == after
            ? null
            : await this.BeginMutationAsync(ownerUserId, cancellationToken);
    }

    public async Task<ShareSourceMutationLease> BeginMutationAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        return await this.sourceRevisionRepository.BeginMutationAsync(
            PersonalRankingShareSourceScope.Create(ownerUserId),
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
                "Unable to settle the personal ranking identity source mutation for {ScopeKey}; its lease will expire conservatively.",
                mutationLease.ScopeKey);
        }
    }
}
