using Dtos.Images;
using Dtos.Images.Links;
using Entities.Model.Images;
using OneOf;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Interfaces.Images
{
    public interface IImageLinksService
    {
        Task<OneOf<ImageDto, ErrorDetail>> LinkImageAsync(LinkImageToOwnerDto request);

        Task<OneOf<ImageDto, ErrorDetail>> GetCurrentImageAsync(
            string ownerId,
            ImageOwnerType ownerType,
            ImageCategory category);

        Task<OneOf<IEnumerable<ImageDto>, ErrorDetail>> GetImagesAsync(
            string ownerId,
            ImageOwnerType ownerType,
            ImageCategory category);

        Task<OneOf<ImageDto, ErrorDetail>> SetCurrentImageAsync(string imageId);

        Task<OneOf<bool, ErrorDetail>> DeleteImageAsync(string imageId);
    }
}