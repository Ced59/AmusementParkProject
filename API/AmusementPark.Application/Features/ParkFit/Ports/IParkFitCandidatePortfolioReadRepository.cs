using AmusementPark.Application.Features.ParkFit.Models;

namespace AmusementPark.Application.Features.ParkFit.Ports;

public interface IParkFitCandidatePortfolioReadRepository
{
    Task<ParkFitCandidatePortfolio> LoadAsync(
        string? countryCode,
        int maximumActiveCandidateCount,
        CancellationToken cancellationToken);
}
