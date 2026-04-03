using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Dtos.Pagination;
using Dtos.Parks.Creating;
using Dtos.Parks.ParkGet;
using Dtos.Parks.Parks;
using Dtos.Parks.Updating;
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
        private readonly IParksService parksService;

        public ParksController(IParksService parksService)
        {
            this.parksService = parksService;
        }

        // 🔹 helper : est-ce que l’utilisateur peut voir les parcs non visibles ?
        private bool UserCanSeeNonVisible()
        {
            return User?.IsInRole("ADMIN") == true
                   || User?.IsInRole("MODERATOR") == true;
        }

        [HttpPost]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> CreateParkAsync([FromBody] ParkCreateDto park)
        {
            OneOf<ParkCreatedDto, ErrorCodes.ErrorDetail> parkCreated = await parksService.CreateParkAsync(park)!;

            return ApiResponseHandler.HandleResponse(parkCreated);
        }

        [HttpGet]
        public async Task<IActionResult> GetParkById([FromQuery] string id)
        {
            ParkGetByIdDto dtoId = new() { Id = id };
            OneOf<ParkGettedDto, ErrorCodes.ErrorDetail> park = await parksService.GetParkByIdAsync(dtoId)!;

            return ApiResponseHandler.HandleResponse(park);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetListPaginatedParksAsync(
            [FromQuery][Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
            int page = 1,
            [FromQuery][Range(1, 100, ErrorMessage = "Size must be between 1 and 100")]
            int size = 10)
        {
            bool includeNonVisible = UserCanSeeNonVisible();

            (IEnumerable<ParkDto> parks, PaginationDto pagination) =
                await parksService.GetListParkPaginatedAsync(page, size, includeNonVisible)!;

            return ApiResponseHandler.HandleResponse(parks, pagination);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchParksByNameAsync(
            [FromQuery][Required] string name,
            [FromQuery][Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")] int page = 1,
            [FromQuery][Range(1, 100, ErrorMessage = "Size must be between 1 and 100")] int size = 10)
        {
            bool includeNonVisible = UserCanSeeNonVisible();

            if (string.IsNullOrWhiteSpace(name))
            {
                (IEnumerable<ParkDto> parksFallback, PaginationDto paginationFallback) =
                    await parksService.GetListParkPaginatedAsync(page, size, includeNonVisible)!;

                return ApiResponseHandler.HandleResponse(parksFallback, paginationFallback);
            }

            (IEnumerable<ParkDto> parks, PaginationDto pagination) =
                await parksService.SearchParksByNamePaginatedAsync(name, page, size, includeNonVisible)!;

            return ApiResponseHandler.HandleResponse(parks, pagination);
        }

        [HttpGet("geo-search")]
        public async Task<IActionResult> SearchParksByLocationAsync(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radius)
        {
            OneOf<IEnumerable<ParkDto>, ErrorCodes.ErrorDetail> parks =
                await parksService.SearchParksByLocationAsync(latitude, longitude, radius);
            return ApiResponseHandler.HandleResponse(parks);
        }


        [HttpPatch("{id}/visibility")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> UpdateParkVisibilityAsync(
            [FromRoute] string id,
            [FromBody] ParkVisibilityUpdateDto request)
        {
            OneOf<ParkDto, ErrorCodes.ErrorDetail> result =
                await parksService.UpdateParkVisibilityAsync(id, request.IsVisible)!;

            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> UpdateParkAsync(
            [FromRoute] string id,
            [FromBody] ParkUpdateDto park)
        {
            OneOf<ParkDto, ErrorCodes.ErrorDetail> result =
                await parksService.UpdateParkAsync(id, park)!;

            return ApiResponseHandler.HandleResponse(result);
        }
    }
}
