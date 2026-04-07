using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de lecture des images d'un propriétaire.
/// </summary>
public sealed class GetImagesByOwnerQueryHandler : IQueryHandler<GetImagesByOwnerQuery, ApplicationResult<IReadOnlyCollection<Image>>>
{
    private readonly IImageRepository imageRepository;

    public GetImagesByOwnerQueryHandler(IImageRepository imageRepository)
    {
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<Image>>> HandleAsync(GetImagesByOwnerQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.OwnerId))
        {
            return ApplicationResult<IReadOnlyCollection<Image>>.Failure(ImageApplicationErrors.InvalidOwner());
        }

        IReadOnlyCollection<Image> images = await this.imageRepository.GetByOwnerAsync(query.OwnerType, query.OwnerId.Trim(), query.Category, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<Image>>.Success(images);
    }
}
