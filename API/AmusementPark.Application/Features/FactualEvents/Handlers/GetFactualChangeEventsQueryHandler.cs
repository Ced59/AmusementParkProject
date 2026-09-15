using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.FactualEvents.Queries;
using AmusementPark.Application.Features.FactualEvents.Results;
using AmusementPark.Application.Features.FactualEvents.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Handlers;

public sealed class GetFactualChangeEventsQueryHandler
    : IQueryHandler<GetFactualChangeEventsQuery,
        ApplicationResult<PagedResult<FactualChangeEventAdminResult>>>
{
    private readonly IFactualChangeEventRepository repository;
    private readonly IParkNameReadRepository parkNameReadRepository;
    private readonly IParkItemNameReadRepository parkItemNameReadRepository;

    public GetFactualChangeEventsQueryHandler(
        IFactualChangeEventRepository repository,
        IParkNameReadRepository parkNameReadRepository,
        IParkItemNameReadRepository parkItemNameReadRepository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.parkNameReadRepository = parkNameReadRepository
            ?? throw new ArgumentNullException(nameof(parkNameReadRepository));
        this.parkItemNameReadRepository = parkItemNameReadRepository
            ?? throw new ArgumentNullException(nameof(parkItemNameReadRepository));
    }

    public async Task<ApplicationResult<PagedResult<FactualChangeEventAdminResult>>> HandleAsync(
        GetFactualChangeEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        long skip = ((long)query.Criteria.Paging.Page - 1L)
            * query.Criteria.Paging.PageSize;
        if (query.Criteria.Paging.Page < 1
            || query.Criteria.Paging.PageSize is < 1 or > 100
            || skip > int.MaxValue
            || query.Criteria.Status.HasValue && !Enum.IsDefined(query.Criteria.Status.Value)
            || query.Criteria.TargetType.HasValue && !Enum.IsDefined(query.Criteria.TargetType.Value)
            || query.Criteria.EventType.HasValue && !Enum.IsDefined(query.Criteria.EventType.Value)
            || query.Criteria.Confidence.HasValue && !Enum.IsDefined(query.Criteria.Confidence.Value))
        {
            return ApplicationResult<PagedResult<FactualChangeEventAdminResult>>.Failure(
                FactualEventAdministrationErrors.InvalidSearch());
        }

        PagedResult<FactualChangeEvent> page = await this.repository.SearchAsync(
            query.Criteria,
            cancellationToken);
        string[] parkIds = page.Items
            .SelectMany(static factualEvent => factualEvent.Target.Type == FactualTargetType.Park
                ? new[] { factualEvent.Target.TargetId }
                : factualEvent.Target.ParentParkId is null
                    ? Array.Empty<string>()
                    : new[] { factualEvent.Target.ParentParkId })
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] parkItemIds = page.Items
            .Where(static factualEvent => factualEvent.Target.Type == FactualTargetType.ParkItem)
            .Select(static factualEvent => factualEvent.Target.TargetId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Task<IReadOnlyDictionary<string, string?>> parkNamesTask = parkIds.Length == 0
            ? Task.FromResult<IReadOnlyDictionary<string, string?>>(
                new Dictionary<string, string?>(StringComparer.Ordinal))
            : this.parkNameReadRepository.GetNamesByIdsAsync(parkIds, cancellationToken);
        Task<IReadOnlyDictionary<string, string?>> parkItemNamesTask = parkItemIds.Length == 0
            ? Task.FromResult<IReadOnlyDictionary<string, string?>>(
                new Dictionary<string, string?>(StringComparer.Ordinal))
            : this.parkItemNameReadRepository.GetNamesByIdsAsync(parkItemIds, cancellationToken);
        await Task.WhenAll(parkNamesTask, parkItemNamesTask);

        IReadOnlyDictionary<string, string?> parkNames = await parkNamesTask;
        IReadOnlyDictionary<string, string?> parkItemNames = await parkItemNamesTask;
        FactualChangeEventAdminResult[] items = page.Items
            .Select(factualEvent => FactualChangeEventAdminResultMapper.Map(
                factualEvent,
                parkNames,
                parkItemNames))
            .ToArray();
        return ApplicationResult<PagedResult<FactualChangeEventAdminResult>>.Success(
            new PagedResult<FactualChangeEventAdminResult>(
                items,
                page.Page,
                page.PageSize,
                page.TotalItems));
    }
}
