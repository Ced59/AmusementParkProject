using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de lecture de toutes les images.
/// </summary>
public sealed class GetAllImagesQueryHandler : IQueryHandler<GetAllImagesQuery, ApplicationResult<IReadOnlyCollection<Image>>>
{
    private readonly IImageRepository imageRepository;

    public GetAllImagesQueryHandler(IImageRepository imageRepository)
    {
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<Image>>> HandleAsync(GetAllImagesQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Image> images = await this.imageRepository.GetAllAsync(cancellationToken);
        return ApplicationResult<IReadOnlyCollection<Image>>.Success(images);
    }
}
