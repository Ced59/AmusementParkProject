using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

/// <summary>
/// Handler de lecture de l'image courante d'un propriétaire.
/// </summary>
public sealed class GetCurrentImageQueryHandler : IQueryHandler<GetCurrentImageQuery, ApplicationResult<Image>>
{
    private readonly IImageRepository imageRepository;

    public GetCurrentImageQueryHandler(IImageRepository imageRepository)
    {
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<Image>> HandleAsync(GetCurrentImageQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.OwnerId))
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.InvalidOwner());
        }

        Image? image = await this.imageRepository.GetCurrentByOwnerAsync(query.OwnerType, query.OwnerId.Trim(), query.Category, cancellationToken);
        if (image is null)
        {
            return ApplicationResult<Image>.Failure(ImageApplicationErrors.ImageNotExists());
        }

        return ApplicationResult<Image>.Success(image);
    }
}
