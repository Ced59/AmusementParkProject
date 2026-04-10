using Dtos.Pagination;
using Dtos.Parks.Creating;
using Dtos.Parks.ParkGet;
using Dtos.Parks.Parks;
using Dtos.Parks.Updating;
using Entities.Model.Errors;
using OneOf;

namespace Services.Interfaces
{
    public interface IParksService
    {
        /// <summary>
        ///     Create Park
        /// </summary>
        /// <param name="park">Park to create Infos</param>
        /// <returns>Confirmation created or error</returns>
        Task<OneOf<ParkCreatedDto, ErrorCodes.ErrorDetail>>? CreateParkAsync(ParkCreateDto park);

        /// <summary>
        ///     Get Park by Id
        /// </summary>
        /// <param name="id">Id Guid</param>
        /// <returns>Park or error</returns>
        Task<OneOf<ParkGettedDto, ErrorCodes.ErrorDetail>>? GetParkByIdAsync(ParkGetByIdDto id);

        /// <summary>
        /// Liste paginée des parcs.
        /// includeNonVisible = true => inclut les parcs isVisible = false (ADMIN/MODERATOR)
        /// </summary>
        Task<(IEnumerable<ParkDto>, PaginationDto)>? GetListParkPaginatedAsync(
            int page,
            int pageSize,
            bool includeNonVisible = false);

        /// <summary>
        /// Recherche paginée de parcs par nom.
        /// includeNonVisible = true => inclut isVisible = false (ADMIN/MODERATOR)
        /// </summary>
        Task<(IEnumerable<ParkDto>, PaginationDto)>? SearchParksByNamePaginatedAsync(
            string name,
            int page,
            int pageSize,
            bool includeNonVisible = false);

        /// <summary>
        /// Search park by location
        /// </summary>
        /// <param name="latitude">Latitude of center location</param>
        /// <param name="longitude">Longitude of center location</param>
        /// <param name="radius">Radius in kilometers of searched parks above center</param>
        /// <returns>List of parks in location parameters</returns>
        Task<OneOf<IEnumerable<ParkDto>, ErrorCodes.ErrorDetail>> SearchParksByLocationAsync(double latitude, double longitude, double radius);

        /// <summary>
        /// Met à jour la visibilité d’un parc.
        /// </summary>
        /// <param name="id">Id du parc</param>
        /// <param name="isVisible">Nouvelle valeur de visibilité</param>
        /// <returns>ParkDto mis à jour ou erreur</returns>
        Task<OneOf<ParkDto, ErrorCodes.ErrorDetail>>? UpdateParkVisibilityAsync(string id, bool isVisible);

        Task<OneOf<ParkDto, ErrorCodes.ErrorDetail>> UpdateParkAsync(string id, ParkUpdateDto dto);
    }
}