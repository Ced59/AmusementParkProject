using Dtos.Pagination;
using Dtos.Parks.Creating;
using Dtos.Parks.ParkGet;
using Dtos.Parks.Parks;
using OneOf;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Interfaces;

public interface IParksService
{
    /// <summary>
    ///     Create Park
    /// </summary>
    /// <param name="park">Park to create Infos</param>
    /// <returns>Confirmation created or error</returns>
    Task<OneOf<ParkCreatedDto, ErrorDetail>>? CreateParkAsync(ParkCreateDto park);

    /// <summary>
    ///     Get Park by Id
    /// </summary>
    /// <param name="id">Id Guid</param>
    /// <returns>Park or error</returns>
    Task<OneOf<ParkGettedDto, ErrorDetail>>? GetParkByIdAsync(ParkGetByIdDto id);

    /// <summary>
    ///     Get paginated list of parks
    /// </summary>
    /// <param name="page">Number of page</param>
    /// <param name="pageSize">Size of page</param>
    /// <returns>List of Parks with pagination</returns>
    Task<(IEnumerable<ParkDto>, PaginationDto)>? GetListParkPaginatedAsync(int page, int pageSize);

    /// <summary>
    /// Search park by location
    /// </summary>
    /// <param name="latitude">Latitude of center location</param>
    /// <param name="longitude">Longitude of center location</param>
    /// <param name="radius">Radius in kilometers of searched parks above center</param>
    /// <returns>List of parks in location parameters</returns>
    Task<OneOf<IEnumerable<ParkDto>, ErrorDetail>> SearchParksByLocationAsync(double latitude, double longitude, double radius);
}