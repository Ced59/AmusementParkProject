using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;

namespace AmusementPark.Application.Features.ParkFit.Services;

public sealed class ParkFitCandidatePortfolioLoader
{
    private readonly IParkFitCandidatePortfolioReadRepository repository;

    public ParkFitCandidatePortfolioLoader(
        IParkFitCandidatePortfolioReadRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ParkFitCandidatePortfolio> LoadAsync(
        string? countryCode,
        CancellationToken cancellationToken)
    {
        return await this.repository.LoadAsync(
            countryCode,
            ParkFitSearchLimits.MaximumActiveCandidateCount,
            cancellationToken);
    }
}
