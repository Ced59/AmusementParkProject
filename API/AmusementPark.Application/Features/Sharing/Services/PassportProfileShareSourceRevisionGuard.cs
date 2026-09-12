using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PassportProfileShareSourceRevisionGuard
    : IPassportProfileShareSourceRevisionGuard
{
    private readonly IShareSourceRevisionRepository sourceRevisionRepository;
    private readonly IPassportProfileShareScopeRegistry scopeRegistry;
    private readonly ILogger<PassportProfileShareSourceRevisionGuard> logger;

    public PassportProfileShareSourceRevisionGuard(
        IShareSourceRevisionRepository sourceRevisionRepository,
        IPassportProfileShareScopeRegistry scopeRegistry,
        ILogger<PassportProfileShareSourceRevisionGuard> logger)
    {
        this.sourceRevisionRepository = sourceRevisionRepository
            ?? throw new ArgumentNullException(nameof(sourceRevisionRepository));
        this.scopeRegistry = scopeRegistry ?? throw new ArgumentNullException(nameof(scopeRegistry));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyCollection<ShareSourceMutationLease>> TryBeginMutationAsync(
        string ownerUserId,
        IReadOnlyCollection<(string ParkId, int Year)> segments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(segments);
        List<ShareSourceMutationLease> leases = new List<ShareSourceMutationLease>();
        try
        {
            leases.Add(await this.sourceRevisionRepository.BeginMutationAsync(
                PassportProfileShareSourceScope.CreateCoordination(ownerUserId),
                cancellationToken));
            IReadOnlyCollection<string> scopeKeys = await this.scopeRegistry.ResolveScopeKeysAsync(
                ownerUserId,
                segments,
                cancellationToken);
            foreach (string scopeKey in scopeKeys
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(static value => value, StringComparer.Ordinal))
            {
                leases.Add(await this.sourceRevisionRepository.BeginMutationAsync(
                    scopeKey,
                    cancellationToken));
            }

            return leases;
        }
        catch
        {
            await this.CompleteMutationAsync(leases, false, CancellationToken.None);
            throw;
        }
    }

    public async Task CompleteMutationAsync(
        IReadOnlyCollection<ShareSourceMutationLease> mutationLeases,
        bool sourceChanged,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutationLeases);
        foreach (ShareSourceMutationLease mutationLease in mutationLeases)
        {
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
}
