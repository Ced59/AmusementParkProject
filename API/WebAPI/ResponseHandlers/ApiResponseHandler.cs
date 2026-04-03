using System.Collections.Generic;
using Dtos.Pagination;
using Entities.Model.Errors;
using Microsoft.AspNetCore.Mvc;
using OneOf;

namespace WebAPI.ResponseHandlers
{
    using static ErrorCodes;

    public static class ApiResponseHandler
    {
        public static IActionResult HandleResponse<T>(OneOf<T, ErrorDetail> result)
    {
        return result.Match<IActionResult>(
            success => new OkObjectResult(success),
            error => new ObjectResult(new { error.StatusCode, error.Message })
            {
                StatusCode = error.StatusCode
            }
        );
    }

        public static IActionResult HandleResponse(ErrorDetail error)
    {
        return new ObjectResult(new { error.StatusCode, error.Message })
        {
            StatusCode = error.StatusCode
        };
    }

        public static IActionResult HandleResponse<T>(IEnumerable<T> data, PaginationDto pagination)
    {
        return new OkObjectResult(new { Data = data, Pagination = pagination });
    }
    }
}