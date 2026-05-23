using System;
using System.Linq;
using AmusementPark.Application.Errors;
using AmusementPark.WebAPI.Architecture;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Responses;

/// <summary>
/// Conversion des résultats applicatifs en réponses HTTP RFC 7807.
/// </summary>
internal static class ApplicationResultHttpExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, ApplicationResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        return controller.ToActionResult(result.Errors.First());
    }

    public static IActionResult ToActionResult(this ControllerBase controller, ApplicationResult result)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return controller.Ok();
        }

        return controller.ToActionResult(result.Errors.First());
    }

    private static IActionResult ToActionResult(this ControllerBase controller, ApplicationError error)
    {
        int statusCode = ApplicationResultHttpMapper.ToStatusCode(error.Type);

        if (error.Type == ApplicationErrorType.Validation && error.Details is not null && error.Details.Count > 0)
        {
            ValidationProblemDetails validationProblemDetails = ApiProblemDetailsFactory.CreateValidation(
                controller.HttpContext,
                error.Details,
                error.Code,
                error.Message);

            return ApiProblemDetailsFactory.ToObjectResult(validationProblemDetails);
        }

        string title = ApiProblemDetailsFactory.GetDefaultTitle(statusCode);
        string detail = statusCode == StatusCodes.Status500InternalServerError
            ? ApiProblemDetailsFactory.GetDefaultDetail(statusCode)
            : error.Message;

        ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
            controller.HttpContext,
            statusCode,
            title,
            detail,
            error.Code);

        return ApiProblemDetailsFactory.ToObjectResult(problemDetails);
    }
}
