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

    public async Task<IReadOnlyCollection<ShareSourceMutationLease>> TryBeginMutationAsync(
        string ownerUserId,
        IReadOnlyCollection<(string ParkId, int Year)> segments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(segments);
        List<ShareSourceMutationLease> leases = new List<ShareSourceMutationLease>();
        try
        {
            foreach ((string ParkId, int Year) segment in segments
                         .Distinct()
                         .OrderBy(static value => value.Year)
                         .ThenBy(static value => value.ParkId, StringComparer.Ordinal))
            {
                ShareSourceMutationLease? lease =
                    await this.sourceRevisionRepository.TryBeginMutationAsync(
                        PassportProfileShareSourceScope.CreateSegment(
                            ownerUserId,
                            segment.Year,
                            segment.ParkId),
                        cancellationToken);
                if (lease is not null)
                {
                    leases.Add(lease);
                }
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
