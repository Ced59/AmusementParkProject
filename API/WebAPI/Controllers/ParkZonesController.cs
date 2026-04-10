using Dtos.ParkZones;
using Dtos.ParkZones.Creating;
using Dtos.ParkZones.ParkZones;
using Dtos.ParkZones.Updating;
using Entities.Model.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using Services.Interfaces;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;

namespace WebAPI.Controllers
{
    [ApiController]
    [SwaggerOrder(8)]
    [Route("park-zones")]
    public class ParkZonesController : ControllerBase
    {
        private readonly IParkZonesService parkZonesService;

        public ParkZonesController(IParkZonesService parkZonesService)
        {
            this.parkZonesService = parkZonesService;
        }

        [HttpGet("park/{parkId}")]
        public async Task<IActionResult> GetByParkIdAsync([FromRoute] string parkId)
        {
            OneOf<IEnumerable<ParkZoneDto>, ErrorCodes.ErrorDetail> result = await parkZonesService.GetByParkIdAsync(parkId);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync([FromRoute] string id)
        {
            OneOf<ParkZoneDto, ErrorCodes.ErrorDetail> result = await parkZonesService.GetByIdAsync(id);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpGet("park/{parkId}/explorer")]
        public async Task<IActionResult> GetExplorerAsync([FromRoute] string parkId)
        {
            bool includeNonVisible = User?.IsInRole("ADMIN") == true || User?.IsInRole("MODERATOR") == true;
            OneOf<ParkExplorerDto, ErrorCodes.ErrorDetail> result = await parkZonesService.GetExplorerAsync(parkId, includeNonVisible);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPost]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> CreateAsync([FromBody] ParkZoneCreateDto dto)
        {
            OneOf<ParkZoneDto, ErrorCodes.ErrorDetail> result = await parkZonesService.CreateAsync(dto);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] ParkZoneUpdateDto dto)
        {
            OneOf<ParkZoneDto, ErrorCodes.ErrorDetail> result = await parkZonesService.UpdateAsync(id, dto);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> DeleteAsync([FromRoute] string id)
        {
            OneOf<bool, ErrorCodes.ErrorDetail> result = await parkZonesService.DeleteAsync(id);
            return ApiResponseHandler.HandleResponse(result);
        }
    }
}
