using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class ListMyUserCollectionEntriesQueryHandler : IQueryHandler<
    ListMyUserCollectionEntriesQuery,
    ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>>
{
    private readonly UserCollectionLifecycleService service;

    public ListMyUserCollectionEntriesQueryHandler(UserCollectionLifecycleService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>> HandleAsync(
        ListMyUserCollectionEntriesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.ListAsync(
            query.UserId,
            query.TargetType,
            query.TargetId,
            cancellationToken);
    }
}
