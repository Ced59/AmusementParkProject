using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class GetParkItemsByParkIdQueryHandler : IQueryHandler<GetParkItemsByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkItem>>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;

    public GetParkItemsByParkIdQueryHandler(IParkItemRepository parkItemRepository, ParkItemReferenceValidator parkItemReferenceValidator)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ParkItem>>> HandleAsync(GetParkItemsByParkIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<IReadOnlyCollection<ParkItem>>.Failure(ApplicationErrors.Required(nameof(query.ParkId)));
        }

        ApplicationError? parkError = await this.parkItemReferenceValidator.EnsureParkExistsAsync(query.ParkId, cancellationToken);
        if (parkError is not null)
        {
            return ApplicationResult<IReadOnlyCollection<ParkItem>>.Failure(parkError);
        }

        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByParkIdAsync(query.ParkId.Trim(), query.IncludeHidden, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<ParkItem>>.Success(items);
    }
}
