namespace WebAPI.ResponseHandlers
{
    using Entities.Model.Errors;
    using Microsoft.AspNetCore.Mvc;
    using OneOf;
    using static Entities.Model.Errors.ErrorCodes;

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
    }

}
