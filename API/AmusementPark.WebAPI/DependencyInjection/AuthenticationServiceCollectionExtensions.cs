using System;
using System.Text;
using AmusementPark.Infrastructure.Configuration.Authentication;
using AmusementPark.WebAPI.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Enregistre l'authentification et l'autorisation HTTP.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        JwtSettings jwtSettings = configuration.GetSection("Authentication:Jwt").Get<JwtSettings>() ?? new JwtSettings();
        string? facebookAppId = configuration["Authentication:Facebook:AppId"];
        string? facebookAppSecret = configuration["Authentication:Facebook:AppSecret"];

        services.AddHttpContextAccessor();

        Microsoft.AspNetCore.Authentication.AuthenticationBuilder authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie("ExternalCookies", options =>
            {
                options.Cookie.Name = "ExternalAuth.Cookie";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.SlidingExpiration = false;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                };
            });

        if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
        {
            authenticationBuilder.AddFacebook("Facebook", options =>
            {
                options.SignInScheme = "ExternalCookies";
                options.AppId = facebookAppId;
                options.AppSecret = facebookAppSecret;
                options.CallbackPath = new PathString("/login/auth/facebook-response");
            });
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicyNames.ActivatedUnblockedUser, static policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(ActivatedUnblockedUserRequirement.Instance);
            });
        });

        services.AddScoped<IAuthorizationHandler, ActivatedUnblockedUserAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ActivatedUnblockedUserAuthorizationResultHandler>();

        return services;
    }
}
