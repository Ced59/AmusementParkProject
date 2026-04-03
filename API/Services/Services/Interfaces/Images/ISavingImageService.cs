using System.Threading.Tasks;
using Dtos.Images.Creating;
using Entities.Model.Errors;
using OneOf;
using Services.Models.Images;

namespace Services.Interfaces.Images
{
    public interface ISavingImageService
    {
        Task<OneOf<ImageCreatedDto, ErrorCodes.ErrorDetail>> SaveAsync(ImageCreateDto imageCreateDto);

        Task<OneOf<ImageCreatedDto, ErrorCodes.ErrorDetail>> SaveAsync(ImageSaveRequest request);
    }
}
