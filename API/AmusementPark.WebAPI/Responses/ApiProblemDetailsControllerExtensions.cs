using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Responses;

/// <summary>
/// Extensions MVC pour retourner le contrat d'erreur RFC 7807 unique.
/// </summary>
internal static class ApiProblemDetailsControllerExtensions
{
    public static ObjectResult ToProblemDetailsResult(
        this ControllerBase controller,
        int statusCode,
        string detail,
        string? errorCode = null,
        string? title = null)
    {
        ArgumentNullException.ThrowIfNull(controller);

        ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
            controller.HttpContext,
            statusCode,
            string.IsNullOrWhiteSpace(title) ? ApiProblemDetailsFactory.GetDefaultTitle(statusCode) : title,
            detail,
            errorCode);

        return ApiProblemDetailsFactory.ToObjectResult(problemDetails);
    }

    public static ObjectResult ToNotFoundProblemDetailsResult(this ControllerBase controller, string detail, string? errorCode = null)
    {
        return controller.ToProblemDetailsResult(StatusCodes.Status404NotFound, detail, errorCode);
    }

    public static ObjectResult ToServiceUnavailableProblemDetailsResult(
        this ControllerBase controller,
        string detail,
        string errorCode,
        int retryAfterSeconds)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (retryAfterSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retryAfterSeconds));
        }

        controller.Response.Headers.RetryAfter = retryAfterSeconds.ToString(
            CultureInfo.InvariantCulture);
        ObjectResult result = controller.ToProblemDetailsResult(
            StatusCodes.Status503ServiceUnavailable,
            detail,
            errorCode);
        if (result.Value is ProblemDetails problemDetails)
        {
            problemDetails.Extensions["retryAfterSeconds"] = retryAfterSeconds;
        }

        return result;
    }
}
