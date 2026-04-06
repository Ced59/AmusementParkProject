using System.Collections.Generic;
using System.Threading.Tasks;
using Dtos.ParkFounders.Creating;
using Dtos.ParkFounders.ParkFounders;
using Dtos.ParkFounders.Updating;
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
    [SwaggerOrder(6)]
    [Route("park-founders")]
    public class ParkFoundersController : ControllerBase
    {
        private readonly IParkFoundersService parkFoundersService;

        public ParkFoundersController(IParkFoundersService parkFoundersService)
        {
            this.parkFoundersService = parkFoundersService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            OneOf<IEnumerable<ParkFounderDto>, ErrorCodes.ErrorDetail> result = await parkFoundersService.GetAllAsync();
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync([FromRoute] string id)
        {
            OneOf<ParkFounderDto, ErrorCodes.ErrorDetail> result = await parkFoundersService.GetByIdAsync(id);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPost]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> CreateAsync([FromBody] ParkFounderCreateDto dto)
        {
            OneOf<ParkFounderDto, ErrorCodes.ErrorDetail> result = await parkFoundersService.CreateAsync(dto);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] ParkFounderUpdateDto dto)
        {
            OneOf<ParkFounderDto, ErrorCodes.ErrorDetail> result = await parkFoundersService.UpdateAsync(id, dto);
            return ApiResponseHandler.HandleResponse(result);
        }
    }
}