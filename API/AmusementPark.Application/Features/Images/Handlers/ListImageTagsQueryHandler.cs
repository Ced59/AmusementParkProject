using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de lecture des tags d'images.
/// </summary>
public sealed class ListImageTagsQueryHandler : IQueryHandler<ListImageTagsQuery, ApplicationResult<IReadOnlyCollection<ImageTag>>>
{
    private readonly IImageTagRepository imageTagRepository;

    public ListImageTagsQueryHandler(IImageTagRepository imageTagRepository)
    {
        this.imageTagRepository = imageTagRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ImageTag>>> HandleAsync(ListImageTagsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ImageTag> tags = await this.imageTagRepository.GetAllAsync(cancellationToken);
        if (!query.IncludeInactive)
        {
            tags = tags.Where(static tag => tag.IsActive).ToList();
        }

        return ApplicationResult<IReadOnlyCollection<ImageTag>>.Success(tags);
    }
}
