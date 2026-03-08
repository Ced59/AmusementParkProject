using Dtos.ParkItems.Creating;
using Dtos.ParkItems.ParkItems;
using Dtos.ParkItems.Updating;
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
    [SwaggerOrder(9)]
    [Route("park-items")]
    public class ParkItemsController : ControllerBase
    {
        private readonly IParkItemsService parkItemsService;

        public ParkItemsController(IParkItemsService parkItemsService)
        {
            this.parkItemsService = parkItemsService;
        }

        [HttpGet("park/{parkId}")]
        public async Task<IActionResult> GetByParkIdAsync([FromRoute] string parkId)
        {
            bool includeNonVisible = User?.IsInRole("ADMIN") == true || User?.IsInRole("MODERATOR") == true;
            OneOf<IEnumerable<ParkItemDto>, ErrorCodes.ErrorDetail> result = await parkItemsService.GetByParkIdAsync(parkId, includeNonVisible);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync([FromRoute] string id)
        {
            OneOf<ParkItemDto, ErrorCodes.ErrorDetail> result = await parkItemsService.GetByIdAsync(id);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPost]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> CreateAsync([FromBody] ParkItemCreateDto dto)
        {
            OneOf<ParkItemDto, ErrorCodes.ErrorDetail> result = await parkItemsService.CreateAsync(dto);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] ParkItemUpdateDto dto)
        {
            OneOf<ParkItemDto, ErrorCodes.ErrorDetail> result = await parkItemsService.UpdateAsync(id, dto);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> DeleteAsync([FromRoute] string id)
        {
            OneOf<bool, ErrorCodes.ErrorDetail> result = await parkItemsService.DeleteAsync(id);
            return ApiResponseHandler.HandleResponse(result);
        }
    }
}
