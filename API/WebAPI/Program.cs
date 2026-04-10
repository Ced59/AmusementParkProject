using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using MongoDB.Driver;
using Repositories.Implementations;
using Repositories.Interfaces;
using Services.Implementations;
using Services.Implementations.Authentication;
using Services.Implementations.Images;
using Services.Implementations.Searching;
using Services.Interfaces;
using Services.Interfaces.Authentication;
using Services.Interfaces.Images;
using Services.Interfaces.Searching;
using Services.Interfaces.Settings;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebAPI.Settings.Attributes;
using WebAPI.Features.CaptainCoaster.Services;
using WebAPI.Settings.Email;
using WebAPI.Settings.Images;
using WebAPI.Settings.MongoDB;
using WebAPI.Settings.OAuth;
using WebAPI.Settings.Security;

namespace WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            ConfigureServices(builder);
            WebApplication app = builder.Build();
            await ConfigureApplication(app);

            await app.RunAsync();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Base Services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddHttpClient();
            ConfigureSwagger(builder.Services);

            // Settings
            EmailSettings emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
            LocalAuthenticationSettings localAuthenticationSettings = builder.Configuration.GetSection("Authentication:Local").Get<LocalAuthenticationSettings>() ?? new LocalAuthenticationSettings();
            CorsSettings corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();
            AdminSeedSettings adminSeedSettings = builder.Configuration.GetSection("Initialization:AdminUser").Get<AdminSeedSettings>() ?? new AdminSeedSettings();

            builder.Services.AddSingleton<IEmailSettings>(emailSettings);
            builder.Services.AddSingleton<ILocalAuthenticationSettings>(localAuthenticationSettings);
            builder.Services.AddSingleton<ICorsSettings>(corsSettings);
            builder.Services.AddSingleton(adminSeedSettings);

            ValidateEmailSettings(emailSettings);

            // Services
            builder.Services.AddScoped<IUsersService, UsersService>();
            builder.Services.AddScoped<IParksService, ParksService>();
            builder.Services.AddScoped<IParkFoundersService, ParkFoundersService>();
            builder.Services.AddScoped<IParkOperatorsService, ParkOperatorsService>();
            builder.Services.AddScoped<IAttractionManufacturersService, AttractionManufacturersService>();
            builder.Services.AddScoped<IParkZonesService, ParkZonesService>();
            builder.Services.AddScoped<IParkItemsService, ParkItemsService>();

            builder.Services.AddScoped<IExternalAuthenticationService, ExternalAuthenticationService>();
            builder.Services.AddScoped<IExternalIdentityProviderService, GoogleExternalIdentityProviderService>();
            builder.Services.AddScoped<ILocalAccountTokenService, LocalAccountTokenService>();
            builder.Services.AddScoped<ILocalAccountEmailService, LocalAccountEmailService>();
            RegisterEmailSender(builder.Services, emailSettings);

            builder.Services.AddScoped<ISearchIndexService, SearchIndexService>();
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.Services.AddScoped<ICountriesService, CountriesService>();
            builder.Services.AddSingleton<ICaptainCoasterAdminService, CaptainCoasterAdminService>();

            builder.Services.AddScoped<IUserQueryHandler, UsersMongoQueryHandler>();
            builder.Services.AddScoped<IParksQueryHandler, ParksMongoQueryHandler>();
            builder.Services.AddScoped<IParkFoundersQueryHandler, ParkFoundersMongoQueryHandler>();
            builder.Services.AddScoped<IParkOperatorsQueryHandler, ParkOperatorsMongoQueryHandler>();
            builder.Services.AddScoped<IAttractionManufacturersQueryHandler, AttractionManufacturersMongoQueryHandler>();
            builder.Services.AddScoped<IParkZonesQueryHandler, ParkZonesMongoQueryHandler>();
            builder.Services.AddScoped<IParkItemsQueryHandler, ParkItemsMongoQueryHandler>();
            builder.Services.AddScoped<ISearchQueryHandler, SearchMongoQueryHandler>();
            builder.Services.AddScoped<IImagesQueryHandler, ImagesMongoQueryHandler>();
            builder.Services.AddScoped<IImageTagsQueryHandler, ImageTagsMongoQueryHandler>();
            builder.Services.AddScoped<ICountriesQueryHandler, CountriesMongoQueryHandler>();

            InjectImagesServices(builder);

            // Authentication
            ConfigureAuthentication(builder.Services, builder.Configuration);

            // JWT Settings
            JwtSettings? jwtSettings = builder.Configuration.GetSection("Authentication:Jwt").Get<JwtSettings>();
            builder.Services.AddSingleton<IJwtSettings>(jwtSettings);
            ValidateJwtSettings(jwtSettings);

            // Google OAuth Settings
            GoogleOAuthSettings? googleAuthSettings = builder.Configuration.GetSection("Authentication:Google").Get<GoogleOAuthSettings>();
            builder.Services.AddSingleton<IGoogleOAuthSettings>(googleAuthSettings);

            // MongoDB Settings
            MongoDbSettings? mongoDbSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
            builder.Services.AddSingleton<IMongoDbSettings>(mongoDbSettings);
            ConfigureMongoDb(builder.Services, mongoDbSettings);

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

            builder.Services.AddDistributedMemoryCache(); 
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); 
                options.Cookie.HttpOnly = true; 
                options.Cookie.IsEssential = true; 
            });

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", builderCors =>
                {
                    builderCors.WithOrigins(corsSettings.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();

                    if (corsSettings.AllowCredentials)
                    {
                        builderCors.AllowCredentials();
                    }
                });
            });
        }

        private static void InjectImagesServices(WebApplicationBuilder builder)
        {
            // MinIO Settings
            MinIoSettings? minioSettings = builder.Configuration.GetSection("Images:MinIo").Get<MinIoSettings>();
            builder.Services.AddSingleton<IImageStorageSettings>(minioSettings);

            // Images
            builder.Services.AddSingleton<IMinioClient>(sp =>
                new MinioClient()
                    .WithEndpoint(minioSettings.Endpoint)
                    .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey)
                    .WithSSL(bool.Parse(minioSettings.WithSsl))
                    .Build());

            builder.Services.AddScoped<IImageCompressorService, ImageCompressorService>();
            builder.Services.AddScoped<IImageStorageService, MinioImageStorageService>();
            builder.Services.AddScoped<ISavingImageService, SavingImageService>();
            builder.Services.AddScoped<IImageMetadataExtractorService, ImageMetadataExtractorService>();
            builder.Services.AddScoped<IImageLinksService, ImageLinksService>();
            builder.Services.AddScoped<IUserAvatarService, UserAvatarService>();

            builder.Services.AddSingleton<IWaterMarkService, WatermarkService>(sp =>
                new WatermarkService(
                    fontFamilyName: "Arial",
                    fontSize: 24f,
                    fontColor: SixLabors.ImageSharp.Color.LightGray,
                    margin: 15));
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Amusement Parks Web API", Version = "v1" });
                c.OrderActionsBy(apiDesc =>
                {
                    SwaggerOrderAttribute? orderAttr = apiDesc.CustomAttributes().OfType<SwaggerOrderAttribute>().FirstOrDefault();
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

                string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Add authorization filter to mark protected endpoints
                c.OperationFilter<AddJwtBearerAuthorizationFilter>();
            });

            services.AddScoped<IOperationFilter, AddJwtBearerAuthorizationFilter>();
        }

        public static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            string jwtKey = GetRequiredConfigurationValue(configuration, "Authentication:Jwt:Key");
            string jwtIssuer = GetRequiredConfigurationValue(configuration, "Authentication:Jwt:Issuer");
            string jwtAudience = GetRequiredConfigurationValue(configuration, "Authentication:Jwt:Audience");
            string? facebookAppId = configuration["Authentication:Facebook:AppId"];
            string? facebookAppSecret = configuration["Authentication:Facebook:AppSecret"];

            AuthenticationBuilder authenticationBuilder = services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "JwtBearer";
                    options.DefaultChallengeScheme = "JwtBearer";
                }).AddCookie("ExternalCookies", options =>
                {
                    options.Cookie.Name = "ExternalAuth.Cookie";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                })
                .AddJwtBearer("JwtBearer", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
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
        }

        private static string GetRequiredConfigurationValue(IConfiguration configuration, string key)
        {
            return configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is required.");
        }


        private static void RegisterEmailSender(IServiceCollection services, IEmailSettings emailSettings)
        {
            if (string.Equals(emailSettings.Mode, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IEmailSender, SmtpEmailSender>();
                return;
            }

            services.AddScoped<IEmailSender, ConsoleEmailSender>();
        }

        private static void ValidateEmailSettings(IEmailSettings settings)
        {
            if (!string.Equals(settings.Mode, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.Host))
            {
                throw new InvalidOperationException("Email SMTP host is not configured.");
            }

            if (settings.Port <= 0)
            {
                throw new InvalidOperationException("Email SMTP port is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.FromAddress))
            {
                throw new InvalidOperationException("Email sender address is not configured.");
            }
        }


        private static void ValidateJwtSettings(IJwtSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Key) || string.IsNullOrEmpty(settings.Issuer) ||
                string.IsNullOrEmpty(settings.Audience))
            {
                throw new InvalidOperationException("JWT settings are not properly configured.");
            }
        }

        private static void ConfigureMongoDb(IServiceCollection services, IMongoDbSettings settings)
        {
            services.AddSingleton<IMongoDatabase>(serviceProvider =>
            {
                MongoClient client = new(settings.Url);
                return client.GetDatabase(settings.DatabaseName);
            });
        }

        private static async Task InitializeMongoDbAsync(IHost app)
        {
            using IServiceScope scope = app.Services.CreateScope();

            IMongoDatabase database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
            IMongoDbSettings mongoDbSettings = scope.ServiceProvider.GetRequiredService<IMongoDbSettings>();
            AdminSeedSettings adminSeedSettings = scope.ServiceProvider.GetRequiredService<AdminSeedSettings>();

            await MongoDbInitializer.InitializeCollectionsAsync(
                database,
                mongoDbSettings,
                adminSeedSettings
            );
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
            app.UseStaticFiles();
            app.UseCors("AllowSpecificOrigin");
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            await InitializeMongoDbAsync(app);
        }
    }
}