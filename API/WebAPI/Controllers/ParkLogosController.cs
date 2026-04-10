using Dtos.Parks.Logos;
using Entities.Model.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using Services.Interfaces.Images.Logos;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("parks/{parkId}/logos")]
    public class ParkLogosController : ControllerBase
    {
        private readonly IParkLogosService parkLogosService;

        public ParkLogosController(IParkLogosService parkLogosService)
        {
            this.parkLogosService = parkLogosService;
        }

        /// <summary>
        /// Ajoute un nouveau logo pour un parc et le rend courant.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> AddLogoAsync(
            [FromRoute] string parkId,
            [FromBody] ParkLogoCreateDto request)
        {
            OneOf<ParkLogoDto, ErrorCodes.ErrorDetail> result = await parkLogosService.AddLogoAsync(parkId, request);
            return ApiResponseHandler.HandleResponse(result);
        }

        /// <summary>
        /// Retourne le logo actuel d'un parc.
        /// </summary>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentLogoAsync([FromRoute] string parkId)
        {
            OneOf<ParkLogoDto, ErrorCodes.ErrorDetail> result = await parkLogosService.GetCurrentLogoAsync(parkId);
            return ApiResponseHandler.HandleResponse(result);
        }

        /// <summary>
        /// Retourne tout l'historique des logos d'un parc.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLogosHistoryAsync([FromRoute] string parkId)
        {
            OneOf<IEnumerable<ParkLogoDto>, ErrorCodes.ErrorDetail> result = await parkLogosService.GetLogosHistoryAsync(parkId);
            return ApiResponseHandler.HandleResponse(result);
        }

        /// <summary>
        /// Définit un logo existant comme logo courant.
        /// </summary>
        [HttpPut("{logoId}/set-current")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> SetCurrentLogoAsync([FromRoute] string parkId, [FromRoute] string logoId)
        {
            // parkId n'est pas utilisé dans le service, mais on l'a dans la route
            OneOf<ParkLogoDto, ErrorCodes.ErrorDetail> result = await parkLogosService.SetCurrentLogoAsync(logoId);
            return ApiResponseHandler.HandleResponse(result);
        }

        /// <summary>
        /// Supprime un logo.
        /// Si c'était le logo courant, un autre est promu courant, ou le parc n'a plus de logo.
        /// </summary>
        [HttpDelete("{logoId}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> DeleteLogoAsync([FromRoute] string parkId, [FromRoute] string logoId)
        {
            OneOf<bool, ErrorCodes.ErrorDetail> result = await parkLogosService.DeleteLogoAsync(logoId);
            return ApiResponseHandler.HandleResponse(result);
        }
    }
}