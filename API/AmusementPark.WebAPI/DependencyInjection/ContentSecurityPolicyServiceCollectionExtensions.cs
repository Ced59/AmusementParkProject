using System;
using AmusementPark.WebAPI.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Registers Content-Security-Policy configuration.
/// </summary>
public static class ContentSecurityPolicyServiceCollectionExtensions
{
    public static IServiceCollection AddApiContentSecurityPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ContentSecurityPolicySettings>(configuration.GetSection(ContentSecurityPolicySettings.SectionName));

        return services;
    }
}
