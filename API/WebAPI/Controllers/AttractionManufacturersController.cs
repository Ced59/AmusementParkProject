using Dtos.AttractionManufacturers.AttractionManufacturers;
using Dtos.AttractionManufacturers.Creating;
using Dtos.AttractionManufacturers.Updating;
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
    [Route("attraction-manufacturers")]
    public class AttractionManufacturersController : ControllerBase
    {
        private readonly IAttractionManufacturersService attractionManufacturersService;

        public AttractionManufacturersController(IAttractionManufacturersService attractionManufacturersService)
        {
            this.attractionManufacturersService = attractionManufacturersService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllAsync()
        {
            OneOf<IEnumerable<AttractionManufacturerDto>, ErrorCodes.ErrorDetail> result = await attractionManufacturersService.GetAllAsync();
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync([FromRoute] string id)
        {
            OneOf<AttractionManufacturerDto, ErrorCodes.ErrorDetail> result = await attractionManufacturersService.GetByIdAsync(id);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPost]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> CreateAsync([FromBody] AttractionManufacturerCreateDto dto)
        {
            OneOf<AttractionManufacturerDto, ErrorCodes.ErrorDetail> result = await attractionManufacturersService.CreateAsync(dto);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] AttractionManufacturerUpdateDto dto)
        {
            OneOf<AttractionManufacturerDto, ErrorCodes.ErrorDetail> result = await attractionManufacturersService.UpdateAsync(id, dto);
            return ApiResponseHandler.HandleResponse(result);
        }
    }
}
