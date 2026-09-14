using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Application.Features.ParkFit.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class ListMyParkFitGroupProfilesQueryHandler
    : IQueryHandler<ListMyParkFitGroupProfilesQuery,
        ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>>
{
    private readonly IParkFitGroupProfileRepository repository;

    public ListMyParkFitGroupProfilesQueryHandler(IParkFitGroupProfileRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>> HandleAsync(
        ListMyParkFitGroupProfilesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        string ownerUserId = IdentifierRules.NormalizeRequired(
            query.OwnerUserId,
            nameof(query.OwnerUserId));
        IReadOnlyCollection<ParkFitGroupProfile> profiles =
            await this.repository.ListOwnedAsync(ownerUserId, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<ParkFitGroupProfileResult>>.Success(
            profiles.Select(static profile => profile.ToResult()).ToArray());
    }
}
