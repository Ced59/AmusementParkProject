using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingDiagnosticsReader
{
    Task<RatingDiagnosticsResult> GetDiagnosticsAsync(CancellationToken cancellationToken);
}
