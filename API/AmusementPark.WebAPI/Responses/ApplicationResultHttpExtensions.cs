using System;
using System.Linq;
using AmusementPark.Application.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Responses;

/// <summary>
/// Conversion des résultats applicatifs en réponses HTTP cohérentes avec le contrat legacy.
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
        int statusCode = error.Type switch
        {
            ApplicationErrorType.Validation => StatusCodes.Status400BadRequest,
            ApplicationErrorType.NotFound => StatusCodes.Status404NotFound,
            ApplicationErrorType.RuleViolation => StatusCodes.Status400BadRequest,
            ApplicationErrorType.Conflict => StatusCodes.Status409Conflict,
            ApplicationErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ApplicationErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return new ObjectResult(new
        {
            StatusCode = statusCode,
            Message = error.Message,
        })
        {
            StatusCode = statusCode,
        };
    }
}
