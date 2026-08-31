using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetRatingDiagnosticsQueryHandler
    : IQueryHandler<GetRatingDiagnosticsQuery, ApplicationResult<RatingDiagnosticsResult>>
{
    private readonly IRatingDiagnosticsReader diagnosticsReader;

    public GetRatingDiagnosticsQueryHandler(IRatingDiagnosticsReader diagnosticsReader)
    {
        this.diagnosticsReader = diagnosticsReader;
    }

    public async Task<ApplicationResult<RatingDiagnosticsResult>> HandleAsync(
        GetRatingDiagnosticsQuery query,
        CancellationToken cancellationToken = default)
    {
        RatingDiagnosticsResult diagnostics = await this.diagnosticsReader.GetDiagnosticsAsync(cancellationToken);
        return ApplicationResult<RatingDiagnosticsResult>.Success(diagnostics);
    }
}
