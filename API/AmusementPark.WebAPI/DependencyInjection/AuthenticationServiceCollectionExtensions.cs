using System;
using System.Text;
using AmusementPark.Infrastructure.Configuration.Authentication;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
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
        RefreshTokenCookieSettings refreshTokenCookieSettings = configuration.GetSection(RefreshTokenCookieSettings.SectionName).Get<RefreshTokenCookieSettings>() ?? new RefreshTokenCookieSettings();

        services.AddHttpContextAccessor();
        services.AddSingleton(refreshTokenCookieSettings);
        services.AddScoped<RefreshTokenCookieService>();

        Microsoft.AspNetCore.Authentication.AuthenticationBuilder authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = ParkDataEditorAuthenticationDefaults.PolicyScheme;
                options.DefaultChallengeScheme = ParkDataEditorAuthenticationDefaults.PolicyScheme;
            })
            .AddPolicyScheme(
                ParkDataEditorAuthenticationDefaults.PolicyScheme,
                ParkDataEditorAuthenticationDefaults.PolicyScheme,
                options =>
                {
                    options.ForwardDefaultSelector = static context =>
                    {
                        string authorizationHeader = context.Request.Headers.Authorization.ToString();
                        return authorizationHeader.StartsWith(
                            $"Bearer {ParkDataEditorAuthenticationDefaults.TokenPrefix}",
                            StringComparison.OrdinalIgnoreCase)
                            ? ParkDataEditorAuthenticationDefaults.AuthenticationScheme
                            : JwtBearerDefaults.AuthenticationScheme;
                    };
                })
            .AddScheme<AuthenticationSchemeOptions, ParkDataEditorTokenAuthenticationHandler>(
                ParkDataEditorAuthenticationDefaults.AuthenticationScheme,
                static _ => { })
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
            AuthorizationPolicy authenticatedUserPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(RestrictedParkDataEditorTokenRequirement.Instance)
                .Build();

            options.DefaultPolicy = authenticatedUserPolicy;
            options.FallbackPolicy = authenticatedUserPolicy;

            options.AddPolicy(AuthorizationPolicyNames.ActivatedUnblockedUser, static policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(ActivatedUnblockedUserRequirement.Instance);
            });

            options.AddPolicy(AuthorizationPolicyNames.AdminOrParkDataEditorToken, static policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(AdminOrParkDataEditorTokenRequirement.Instance);
            });

            options.AddPolicy(AuthorizationPolicyNames.ParkDataEditorToken, static policy =>
            {
                policy.AddAuthenticationSchemes(ParkDataEditorAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthorizationRoleGroups.ParkDataEditor);
            });

            options.AddPolicy(AuthorizationPolicyNames.ParkDataEditorJwt, static policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(AuthorizationRoleGroups.ParkDataEditor);
            });
        });

        services.AddScoped<IAuthorizationHandler, ActivatedUnblockedUserAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, RestrictedParkDataEditorTokenAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, AdminOrParkDataEditorTokenAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ActivatedUnblockedUserAuthorizationResultHandler>();

        return services;
    }
}
