using System;
using System.Threading.Tasks;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Adds the configured Content-Security-Policy header to API responses.
/// </summary>
public static class ContentSecurityPolicyApplicationBuilderExtensions
{
    private const string ContentSecurityPolicyHeaderName = "Content-Security-Policy";
    private const string ContentSecurityPolicyReportOnlyHeaderName = "Content-Security-Policy-Report-Only";

    public static WebApplication UseApiContentSecurityPolicy(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        ContentSecurityPolicySettings settings = app.Services
            .GetRequiredService<IOptions<ContentSecurityPolicySettings>>()
            .Value;

        if (!settings.Enabled)
        {
            return app;
        }

        string headerValue = ContentSecurityPolicyHeaderBuilder.Build(settings);
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return app;
        }

        string headerName = settings.ReportOnly
            ? ContentSecurityPolicyReportOnlyHeaderName
            : ContentSecurityPolicyHeaderName;

        app.Use(async (HttpContext context, RequestDelegate next) =>
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(headerName))
                {
                    context.Response.Headers.Append(headerName, new StringValues(headerValue));
                }

                return Task.CompletedTask;
            });

            await next(context);
        });

        return app;
    }
}
