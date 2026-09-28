using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Handlers;

public sealed class GetAdminLiveTargetMappingsQueryHandler
    : IQueryHandler<
        GetAdminLiveTargetMappingsQuery,
        ApplicationResult<PagedResult<LiveTargetMappingResult>>>
{
    private readonly ILiveTargetMappingRepository repository;

    public GetAdminLiveTargetMappingsQueryHandler(ILiveTargetMappingRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<PagedResult<LiveTargetMappingResult>>> HandleAsync(
        GetAdminLiveTargetMappingsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        LiveTargetMappingSearchCriteria criteria = query.Criteria;
        long skip = ((long)criteria.Page - 1L) * criteria.PageSize;
        if (criteria.Page < 1
            || criteria.PageSize is < 1 or > 100
            || skip > int.MaxValue
            || criteria.Status.HasValue && !Enum.IsDefined(criteria.Status.Value)
            || criteria.TargetType.HasValue && !Enum.IsDefined(criteria.TargetType.Value)
            || criteria.SourceId?.Trim().Length > 256
            || criteria.Search?.Trim().Length > 200)
        {
            return ApplicationResult<PagedResult<LiveTargetMappingResult>>.Failure(
                LiveDataApplicationErrors.InvalidSearch());
        }

        PagedResult<ExternalLiveTargetMapping> page = await this.repository.SearchLatestAsync(
            criteria,
            cancellationToken);
        LiveTargetMappingResult[] items = page.Items
            .Select(LiveTargetMappingResultFactory.Create)
            .ToArray();
        return ApplicationResult<PagedResult<LiveTargetMappingResult>>.Success(
            new PagedResult<LiveTargetMappingResult>(
                items,
                page.Page,
                page.PageSize,
                page.TotalItems));
    }
}
