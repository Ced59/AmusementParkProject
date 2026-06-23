using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Application.Features.TechnicalStats.Ports;
using AmusementPark.Application.Features.TechnicalStats.Queries;

namespace AmusementPark.Application.Features.TechnicalStats.Handlers;

public sealed class GetTechnicalStatsQueryHandler
    : IQueryHandler<GetTechnicalStatsQuery, ApplicationResult<TechnicalStatsSnapshot>>
{
    private readonly ITechnicalStatsProvider provider;

    public GetTechnicalStatsQueryHandler(ITechnicalStatsProvider provider)
    {
        this.provider = provider;
    }

    public async Task<ApplicationResult<TechnicalStatsSnapshot>> HandleAsync(
        GetTechnicalStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        TechnicalStatsSnapshot? snapshot = await this.provider.GetSnapshotAsync(cancellationToken);

        if (snapshot is null)
        {
            snapshot = new TechnicalStatsSnapshot
            {
                IsAvailable = false,
                GeneratedAtUtc = DateTime.UtcNow,
                StartedAtUtc = DateTime.UtcNow,
                UptimeSeconds = 0
            };
        }

        return ApplicationResult<TechnicalStatsSnapshot>.Success(snapshot);
    }
}
