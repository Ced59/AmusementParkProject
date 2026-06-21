using System;
using System.Threading.Tasks;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.AdminPublicView;

public sealed class AdminPublicViewSimulationMiddleware
{
    private readonly RequestDelegate next;

    public AdminPublicViewSimulationMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!AdminPublicViewSimulation.HasRequestHeader(context.Request.Headers))
        {
            await this.next(context);
            return;
        }

        if (!AdminPublicViewSimulation.TryReadRequestMode(context.Request.Headers, out AdminPublicViewSimulationMode mode, out string? invalidValue))
        {
            AdminPublicViewSimulation.ApplyNoStoreHeaders(context);
            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "admin-public-view-simulation.invalid",
                $"Public view simulation mode '{invalidValue ?? string.Empty}' is not supported.");
            return;
        }

        if (context.User?.IsInRole("ADMIN") != true)
        {
            AdminPublicViewSimulation.ApplyNoStoreHeaders(context);
            await WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                "admin-public-view-simulation.forbidden",
                "Only a real admin session can request a simulated public view.");
            return;
        }

        AdminPublicViewSimulation.SetAppliedMode(context, mode);
        AdminPublicViewSimulation.ApplyNoStoreHeaders(context);
        context.Response.OnStarting(static state =>
        {
            HttpContext httpContext = (HttpContext)state;
            AdminPublicViewSimulation.ApplyNoStoreHeaders(httpContext);
            return Task.CompletedTask;
        }, context);

        await this.next(context);
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string errorCode, string detail)
    {
        ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
            context,
            statusCode,
            ApiProblemDetailsFactory.GetDefaultTitle(statusCode),
            detail,
            errorCode);

        return ApiProblemDetailsFactory.WriteAsync(context, problemDetails);
    }
}
