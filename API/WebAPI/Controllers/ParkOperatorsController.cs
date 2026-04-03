using System.Collections.Generic;
using System.Threading.Tasks;
using OneOf;
using Dtos.ParkOperators.Creating;
using Dtos.ParkOperators.ParkOperators;
using Dtos.ParkOperators.Updating;
using Entities.Model.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;

namespace WebAPI.Controllers
{
    [ApiController]
    [SwaggerOrder(7)]
    [Route("park-operators")]
    public class ParkOperatorsController : ControllerBase
    {
        private readonly IParkOperatorsService parkOperatorsService;

        public ParkOperatorsController(IParkOperatorsService parkOperatorsService)
        {
            this.parkOperatorsService = parkOperatorsService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllAsync()
        {
            OneOf<IEnumerable<ParkOperatorDto>, ErrorCodes.ErrorDetail> result = await parkOperatorsService.GetAllAsync();
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync([FromRoute] string id)
        {
            OneOf<ParkOperatorDto, ErrorCodes.ErrorDetail> result = await parkOperatorsService.GetByIdAsync(id);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPost]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> CreateAsync([FromBody] ParkOperatorCreateDto dto)
        {
            OneOf<ParkOperatorDto, ErrorCodes.ErrorDetail> result = await parkOperatorsService.CreateAsync(dto);
            return ApiResponseHandler.HandleResponse(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "MODERATOR,ADMIN")]
        [RequireActivatedUnblockedUser]
        public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] ParkOperatorUpdateDto dto)
        {
            OneOf<ParkOperatorDto, ErrorCodes.ErrorDetail> result = await parkOperatorsService.UpdateAsync(id, dto);
            return ApiResponseHandler.HandleResponse(result);
        }
    }
}