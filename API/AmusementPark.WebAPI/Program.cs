using System.Text;
using AmusementPark.Application;
using AmusementPark.Application.DependencyInjection;
using AmusementPark.Infrastructure;
using AmusementPark.Infrastructure.Configuration.Authentication;
using AmusementPark.Infrastructure.DependencyInjection;
using AmusementPark.Infrastructure.Persistence.Mongo.Projections;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddApplicationHandlers(static type =>
{
    string? namespaceName = type.Namespace;
    if (string.IsNullOrWhiteSpace(namespaceName))
    {
        return false;
    }

    return namespaceName.Contains(".Features.Countries.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.ParkFounders.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.ParkOperators.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.AttractionManufacturers.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.Parks.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.ParkZones.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.ParkItems.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.Images.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.Users.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.Search.", StringComparison.Ordinal);
});
builder.Services.AddInfrastructure(builder.Configuration);

JwtSettings jwtSettings = builder.Configuration.GetSection("Authentication:Jwt").Get<JwtSettings>() ?? new JwtSettings();
string? facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
string? facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

Microsoft.AspNetCore.Authentication.AuthenticationBuilder authenticationBuilder = builder.Services
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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    MongoSearchProjectionInitializer searchProjectionInitializer = scope.ServiceProvider.GetRequiredService<MongoSearchProjectionInitializer>();
    await searchProjectionInitializer.InitializeAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    architecture = "clean-architecture-phase-11",
    application = AmusementPark.Application.ArchitecturePhase.Current,
    infrastructure = AmusementPark.Infrastructure.ArchitecturePhase.Current,
    project = "AmusementPark.WebAPI",
    migratedFeatures = new[]
    {
        "Countries",
        "ParkFounders",
        "ParkOperators",
        "AttractionManufacturers",
        "Parks",
        "ParkZones",
        "ParkItems",
        "Images",
        "Users",
        "Auth",
        "Search",
    },
}));

app.Run();
