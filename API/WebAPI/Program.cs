using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Repositories.Implementations;
using Repositories.Interfaces;
using Services.Implementations;
using Services.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebAPI.Settings.Attributes;
using WebAPI.Settings.MongoDB;
using WebAPI.Settings.Security;

//using WebAPI.Middlewares;

namespace WebAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder);
        var app = builder.Build();
        await ConfigureApplication(app);

        await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Base Services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        ConfigureSwagger(builder.Services);

        // Authentication
        ConfigureAuthentication(builder.Services, builder.Configuration);

        // JWT Settings
        var jwtSettings = builder.Configuration.GetSection("Authentication:Jwt").Get<JwtSettings>();
        builder.Services.AddSingleton<IJwtSettings>(jwtSettings);
        ValidateJwtSettings(jwtSettings);

        // MongoDB Settings
        var mongoDbSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
        builder.Services.AddSingleton<IMongoDbSettings>(mongoDbSettings);
        ConfigureMongoDb(builder.Services, mongoDbSettings);

        // Services
        builder.Services.AddScoped<IUsersService, UsersService>();
        builder.Services.AddScoped<IUserQueryHandler, UsersMongoQueryHandler>();

        // Other Configurations
        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Configure IpRateLimiting
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
        builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin",
                builderCors => builderCors.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
    }

    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Amusement Parks Web API", Version = "v1" });
            c.OrderActionsBy(apiDesc =>
            {
                var orderAttr = apiDesc.CustomAttributes().OfType<SwaggerOrderAttribute>().FirstOrDefault();
                return orderAttr != null ? orderAttr.Order.ToString() : int.MaxValue.ToString();
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description =
                    "Please enter JWT bearer token in the Authorization header using the Bearer scheme. Example: Bearer {token}",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

            // Add authorization filter to mark protected endpoints
            c.OperationFilter<AddJwtBearerAuthorizationFilter>();
        });

        services.AddScoped<IOperationFilter, AddJwtBearerAuthorizationFilter>();
    }

    public static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "JwtBearer";
                options.DefaultChallengeScheme = "JwtBearer";
            })
            .AddJwtBearer("JwtBearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Authentication:Jwt:Key"])),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Authentication:Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Authentication:Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(JsonSerializer.Serialize(new
                            { message = "Authentication failed" }));
                    },
                    OnChallenge = context =>
                    {
                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            return context.Response.WriteAsync(JsonSerializer.Serialize(new
                                { message = "Access denied. You must be logged in." }));
                        }

                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(JsonSerializer.Serialize(new
                            { message = "You do not have permission to access this resource." }));
                    }
                };
            })
            .AddGoogle("Google", options =>
            {
                options.SignInScheme = "ExternalCookies";
                options.ClientId = configuration["Authentication:Google:ClientId"];
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                options.CallbackPath = new PathString("/login/auth/google-response");
            })
            .AddFacebook("Facebook", options =>
            {
                options.SignInScheme = "ExternalCookies";
                options.AppId = configuration["Authentication:Facebook:AppId"];
                options.AppSecret = configuration["Authentication:Facebook:AppSecret"];
                options.CallbackPath = new PathString("/login/auth/facebook-response");
            })
            .AddCookie("ExternalCookies", options =>
            {
                options.Cookie.Name = "ExternalAuth.Cookie";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
    }


    private static void ValidateJwtSettings(IJwtSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Key) || string.IsNullOrEmpty(settings.Issuer) ||
            string.IsNullOrEmpty(settings.Audience))
            throw new InvalidOperationException("JWT settings are not properly configured.");
    }

    private static void ConfigureMongoDb(IServiceCollection services, IMongoDbSettings settings)
    {
        services.AddSingleton<IMongoDatabase>(serviceProvider =>
        {
            var client = new MongoClient(settings.Url);
            return client.GetDatabase(settings.DatabaseName);
        });
    }

    private static async Task InitializeMongoDbAsync(IHost app)
    {
        var database = app.Services.GetRequiredService<IMongoDatabase>();
        var mongoDbSettings = app.Services.GetRequiredService<IMongoDbSettings>();
        await MongoDbInitializer.InitializeCollectionsAsync(database, mongoDbSettings);
    }

    private static async Task ConfigureApplication(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //app.UseMiddleware<JwtMiddleware>();
        app.UseIpRateLimiting();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseCors("AllowSpecificOrigin");


        await InitializeMongoDbAsync(app);
    }
}