using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Ports;

/// <summary>
/// Port applicatif de persistance des tags d'images.
/// </summary>
public interface IImageTagRepository
{
    Task<IReadOnlyCollection<ImageTag>> GetAllAsync(CancellationToken cancellationToken);
    Task<ImageTag> CreateAsync(ImageTagWriteModel tag, CancellationToken cancellationToken);
    Task<ImageTag?> UpdateAsync(string tagId, ImageTagWriteModel tag, CancellationToken cancellationToken);
}
