using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération du résumé public optimisé d'un parc.
/// </summary>
public sealed class GetParkDetailSummaryQueryHandler : IQueryHandler<GetParkDetailSummaryQuery, ApplicationResult<ParkDetailSummaryResult>>
{
    private readonly IParkDetailSummaryReadRepository repository;

    public GetParkDetailSummaryQueryHandler(IParkDetailSummaryReadRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<ParkDetailSummaryResult>> HandleAsync(GetParkDetailSummaryQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<ParkDetailSummaryResult>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        ParkDetailSummaryResult? summary = await this.repository.GetAsync(query.ParkId.Trim(), query.IncludeHidden, query.ClosedFilter, cancellationToken);
        if (summary is null)
        {
            return ApplicationResult<ParkDetailSummaryResult>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        return ApplicationResult<ParkDetailSummaryResult>.Success(summary);
    }
}
