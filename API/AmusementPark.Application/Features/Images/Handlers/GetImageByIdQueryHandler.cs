using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de lecture d'image par identifiant.
/// </summary>
public sealed class GetImageByIdQueryHandler : IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>>
{
    private readonly IImageRepository imageRepository;

    public GetImageByIdQueryHandler(IImageRepository imageRepository)
    {
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<Image>> HandleAsync(GetImageByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ImageId))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
        }

        Image? image = await this.imageRepository.GetByIdAsync(query.ImageId.Trim(), cancellationToken);
        if (image is null)
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
        }

        return ApplicationResult<Image>.Success(image);
    }
}
