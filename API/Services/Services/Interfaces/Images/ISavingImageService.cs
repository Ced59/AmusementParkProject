using Dtos.Images.Creating;
using Entities.Model.Errors;
using OneOf;

namespace Services.Interfaces.Images;

public interface ISavingImageService
{
    public Task<OneOf<ImageCreatedDto, ErrorCodes.ErrorDetail>> SaveAsync(ImageCreateDto imageCreateDto);
}