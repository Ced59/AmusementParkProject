using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AmusementPark.WebAPI.Responses;

/// <summary>
/// Fabrique unique des réponses d'erreur HTTP RFC 7807 de l'API.
/// </summary>
public static class ApiProblemDetailsFactory
{
    public const string ProblemJsonContentType = "application/problem+json";

    public static ProblemDetails Create(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string? errorCode = null,
        string? type = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        ProblemDetails problemDetails = new ProblemDetails
        {
            Type = string.IsNullOrWhiteSpace(type) ? GetDefaultType(statusCode) : type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path.Value,
        };

        Enrich(problemDetails, httpContext, errorCode);
        return problemDetails;
    }

    public static ValidationProblemDetails CreateValidation(
        HttpContext httpContext,
        ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(modelState);

        Dictionary<string, IReadOnlyCollection<string>> errors = modelState
            .Where(static item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                static item => item.Key,
                static item => (IReadOnlyCollection<string>)item.Value!.Errors
                    .Select(static error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The submitted value is invalid."
                        : error.ErrorMessage)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return CreateValidation(httpContext, errors, "validation.model-state.invalid");
    }

    public static ValidationProblemDetails CreateValidation(
        HttpContext httpContext,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> errors,
        string? errorCode = null,
        string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(errors);

        Dictionary<string, string[]> normalizedErrors = errors.ToDictionary(
            static item => item.Key,
            static item => item.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

        ValidationProblemDetails problemDetails = new ValidationProblemDetails(normalizedErrors)
        {
            Type = GetDefaultType(StatusCodes.Status400BadRequest),
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest,
            Detail = string.IsNullOrWhiteSpace(detail) ? "One or more validation errors occurred." : detail,
            Instance = httpContext.Request.Path.Value,
        };

        Enrich(problemDetails, httpContext, errorCode ?? "validation.failed");
        return problemDetails;
    }

    public static ObjectResult ToObjectResult(ProblemDetails problemDetails)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);

        ObjectResult result = new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError,
        };

        result.ContentTypes.Add(ProblemJsonContentType);
        return result;
    }

    public static Task WriteAsync(HttpContext httpContext, ProblemDetails problemDetails, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(problemDetails);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = ProblemJsonContentType;
        return httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: ProblemJsonContentType, cancellationToken: cancellationToken);
    }

    public static string GetDefaultTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad request.",
            StatusCodes.Status401Unauthorized => "Authentication is required.",
            StatusCodes.Status403Forbidden => "Access is forbidden.",
            StatusCodes.Status404NotFound => "Resource not found.",
            StatusCodes.Status409Conflict => "Conflict.",
            StatusCodes.Status415UnsupportedMediaType => "Unsupported media type.",
            StatusCodes.Status429TooManyRequests => "Too many requests.",
            StatusCodes.Status500InternalServerError => "Unexpected server error.",
            _ => "HTTP error.",
        };
    }

    public static string GetDefaultDetail(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "The request cannot be processed as submitted.",
            StatusCodes.Status401Unauthorized => "You must be authenticated to access this resource.",
            StatusCodes.Status403Forbidden => "You do not have permission to access this resource.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status409Conflict => "The request conflicts with the current resource state.",
            StatusCodes.Status415UnsupportedMediaType => "The request media type is not supported.",
            StatusCodes.Status429TooManyRequests => "Too many requests were sent in a short period. Please retry later.",
            StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
            _ => "The request failed.",
        };
    }

    private static void Enrich(ProblemDetails problemDetails, HttpContext httpContext, string? errorCode)
    {
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            problemDetails.Extensions["errorCode"] = errorCode;
        }
    }

    private static string GetDefaultType(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "https://amusement-parks.fun/problems/bad-request",
            StatusCodes.Status401Unauthorized => "https://amusement-parks.fun/problems/authentication-required",
            StatusCodes.Status403Forbidden => "https://amusement-parks.fun/problems/access-forbidden",
            StatusCodes.Status404NotFound => "https://amusement-parks.fun/problems/not-found",
            StatusCodes.Status409Conflict => "https://amusement-parks.fun/problems/conflict",
            StatusCodes.Status415UnsupportedMediaType => "https://amusement-parks.fun/problems/unsupported-media-type",
            StatusCodes.Status429TooManyRequests => "https://amusement-parks.fun/problems/rate-limit-exceeded",
            StatusCodes.Status500InternalServerError => "https://amusement-parks.fun/problems/unexpected-error",
            _ => "https://amusement-parks.fun/problems/http-error",
        };
    }
}
