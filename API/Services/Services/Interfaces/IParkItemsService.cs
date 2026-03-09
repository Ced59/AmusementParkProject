using Dtos.Pagination;
using Dtos.ParkItems.Creating;
using Dtos.ParkItems.ParkItems;
using Dtos.ParkItems.Updating;
using Entities.Model.Errors;
using OneOf;

namespace Services.Interfaces
{
    public interface IParkItemsService
    {
        Task<OneOf<IEnumerable<ParkItemDto>, ErrorCodes.ErrorDetail>> GetByParkIdAsync(string parkId, bool includeNonVisible = true);
        Task<(IEnumerable<ParkItemAdminListDto> Data, PaginationDto Pagination)> GetPaginatedAsync(
            int page,
            int pageSize,
            string? parkId,
            string? search,
            bool includeNonVisible = true);
        Task<OneOf<ParkItemDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id);
        Task<OneOf<ParkItemDto, ErrorCodes.ErrorDetail>> CreateAsync(ParkItemCreateDto dto);
        Task<OneOf<ParkItemDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkItemUpdateDto dto);
        Task<OneOf<bool, ErrorCodes.ErrorDetail>> DeleteAsync(string id);
    }
}
