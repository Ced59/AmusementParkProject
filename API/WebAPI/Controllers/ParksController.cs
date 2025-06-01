using System.ComponentModel.DataAnnotations;
using Dtos.Pagination;
using Dtos.Parks.Creating;
using Dtos.Parks.ParkGet;
using Dtos.Parks.Parks;
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
    [SwaggerOrder(1)]
    [Route("[controller]")]
    public class ParksController : ControllerBase
    {
        private readonly IParksService _parksService;

        public ParksController(IParksService parksService)
        {
            _parksService = parksService;
        }


        [HttpPost]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> CreateParkAsync([FromBody] ParkCreateDto park)
        {
            OneOf<ParkCreatedDto, ErrorCodes.ErrorDetail> parkCreated = await _parksService.CreateParkAsync(park)!;

            return ApiResponseHandler.HandleResponse(parkCreated);
        }

        [HttpGet]
        public async Task<IActionResult> GetParkById([FromQuery] string id)
        {
            ParkGetByIdDto dtoId = new() { Id = id };
            OneOf<ParkGettedDto, ErrorCodes.ErrorDetail> park = await _parksService.GetParkByIdAsync(dtoId)!;

            return ApiResponseHandler.HandleResponse(park);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetListPaginatedParksAsync(
            [FromQuery] [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
            int page = 1,
            [FromQuery] [Range(1, 100, ErrorMessage = "Size must be between 1 and 100")]
            int size = 10)
        {
            (IEnumerable<ParkDto> parks, PaginationDto pagination) = await _parksService.GetListParkPaginatedAsync(page, size)!;
            return ApiResponseHandler.HandleResponse(parks, pagination);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchParksByLocationAsync(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radius)
        {
            OneOf<IEnumerable<ParkDto>, ErrorCodes.ErrorDetail> parks = await _parksService.SearchParksByLocationAsync(latitude, longitude, radius);
            return ApiResponseHandler.HandleResponse(parks);
        }

    }
}