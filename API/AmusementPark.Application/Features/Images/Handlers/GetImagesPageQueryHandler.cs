using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de lecture paginée des images administrables.
/// </summary>
public sealed class GetImagesPageQueryHandler : IQueryHandler<GetImagesPageQuery, ApplicationResult<PagedResult<Image>>>
{
    private readonly IImageRepository imageRepository;

    public GetImagesPageQueryHandler(IImageRepository imageRepository)
    {
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<PagedResult<Image>>> HandleAsync(GetImagesPageQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Paging.Page <= 0 || query.Paging.PageSize <= 0)
        {
            return ApplicationResult<PagedResult<Image>>.Failure(ApplicationErrors.InvalidPagination());
        }

        PagedResult<Image> page = await this.imageRepository.GetPageAsync(query.Paging.Page, query.Paging.PageSize, query.Criteria, cancellationToken);
        return ApplicationResult<PagedResult<Image>>.Success(page);
    }
}
