using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using MongoDB.Driver;
using Repositories.Implementations;
using Repositories.Interfaces;
using Services.Configuration;
using Services.Implementations;
using Services.Implementations.Images;
using Services.Implementations.Searching;
using Services.Interfaces;
using Services.Interfaces.Images;
using Services.Interfaces.Searching;
using Services.Interfaces.Settings;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebAPI.Settings.Attributes;
using WebAPI.Settings.Images;
using WebAPI.Settings.MongoDB;
using WebAPI.Settings.OAuth;
using WebAPI.Settings.Security;

namespace WebAPI
{
    public class Program
    {
        private const string JwtAuthenticationExceptionItemKey = "JwtAuthenticationException";
        private const string CorsPolicyName = "AllowSpecificOrigin";
        private const string ExternalCookiesScheme = "ExternalCookies";

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
            // Base services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();

            ConfigureSwagger(builder.Services);

            // Settings
            JwtSettings jwtSettings = GetRequiredSettings<JwtSettings>(builder.Configuration, "Authentication:Jwt");
            GoogleOAuthSettings googleOAuthSettings = GetRequiredSettings<GoogleOAuthSettings>(builder.Configuration, "Authentication:Google");
            MongoDbSettings mongoDbSettings = GetRequiredSettings<MongoDbSettings>(builder.Configuration, "MongoDB");
            MinIoSettings minIoSettings = GetRequiredSettings<MinIoSettings>(builder.Configuration, "Images:MinIo");

            ValidateJwtSettings(jwtSettings);
            ValidateMongoDbSettings(mongoDbSettings);
            ValidateMinIoSettings(minIoSettings);

            builder.Services.AddSingleton<IJwtSettings>(jwtSettings);
            builder.Services.AddSingleton<IGoogleOAuthSettings>(googleOAuthSettings);
            builder.Services.AddSingleton<IMongoDbSettings>(mongoDbSettings);
            builder.Services.AddSingleton<IImageStorageSettings>(minIoSettings);

            // Application services
            builder.Services.AddScoped<IUsersService, UsersService>();
            builder.Services.AddScoped<IParksService, ParksService>();
            builder.Services.AddScoped<IParkFoundersService, ParkFoundersService>();
            builder.Services.AddScoped<IParkOperatorsService, ParkOperatorsService>();
            builder.Services.AddScoped<IParkZonesService, ParkZonesService>();
            builder.Services.AddScoped<IParkItemsService, ParkItemsService>();

            builder.Services.AddScoped<ISocialAuthService, SocialAuthService>();

            builder.Services.AddScoped<ISearchIndexService, SearchIndexService>();
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.Services.AddScoped<ICountriesService, CountriesService>();

            builder.Services.AddScoped<IUserQueryHandler, UsersMongoQueryHandler>();
            builder.Services.AddScoped<IParksQueryHandler, ParksMongoQueryHandler>();
            builder.Services.AddScoped<IParkFoundersQueryHandler, ParkFoundersMongoQueryHandler>();
            builder.Services.AddScoped<IParkOperatorsQueryHandler, ParkOperatorsMongoQueryHandler>();
            builder.Services.AddScoped<IParkZonesQueryHandler, ParkZonesMongoQueryHandler>();
            builder.Services.AddScoped<IParkItemsQueryHandler, ParkItemsMongoQueryHandler>();
            builder.Services.AddScoped<ISearchQueryHandler, SearchMongoQueryHandler>();
            builder.Services.AddScoped<IImagesQueryHandler, ImagesMongoQueryHandler>();
            builder.Services.AddScoped<ICountriesQueryHandler, CountriesMongoQueryHandler>();

            InjectImagesServices(builder.Services, minIoSettings);

            // MongoDB
            ConfigureMongoDb(builder.Services, mongoDbSettings);

            // Authentication / Authorization
            ConfigureAuthentication(builder.Services, builder.Configuration);
            builder.Services.AddAuthorization();

            // MVC / JSON / Routing
            builder.Services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Rate limiting
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
            builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // Session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, corsPolicyBuilder =>
                {
                    corsPolicyBuilder
                        .WithOrigins(
                            "http://localhost:4200",
                            "https://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        private static void InjectImagesServices(IServiceCollection services, MinIoSettings minIoSettings)
        {
            services.AddSingleton<IMinioClient>(_ =>
                new MinioClient()
                    .WithEndpoint(minIoSettings.Endpoint)
                    .WithCredentials(minIoSettings.AccessKey, minIoSettings.SecretKey)
                    .WithSSL(bool.Parse(minIoSettings.WithSsl))
                    .Build());

            services.AddScoped<IImageCompressorService, ImageCompressorService>();
            services.AddScoped<IImageStorageService, MinioImageStorageService>();
            services.AddScoped<ISavingImageService, SavingImageService>();
            services.AddScoped<IImageMetadataExtractorService, ImageMetadataExtractorService>();
            services.AddScoped<IImageLinksService, ImageLinksService>();

            services.AddSingleton<IWaterMarkService, WatermarkService>(_ =>
                new WatermarkService(
                    fontFamilyName: "Arial",
                    fontSize: 24f,
                    fontColor: SixLabors.ImageSharp.Color.LightGray,
                    margin: 15));
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Amusement Parks Web API",
                    Version = "v1"
                });

                options.OrderActionsBy(apiDescription =>
                {
                    SwaggerOrderAttribute? orderAttribute = apiDescription.CustomAttributes()
                        .OfType<SwaggerOrderAttribute>()
                        .FirstOrDefault();

                    return orderAttribute != null
                        ? orderAttribute.Order.ToString("D10")
                        : int.MaxValue.ToString("D10");
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                options.OperationFilter<AddJwtBearerAuthorizationFilter>();
            });
        }

        public static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            string jwtKey = configuration["Authentication:Jwt:Key"]
                ?? throw new InvalidOperationException("Authentication:Jwt:Key is missing.");

            string jwtIssuer = configuration["Authentication:Jwt:Issuer"]
                ?? throw new InvalidOperationException("Authentication:Jwt:Issuer is missing.");

            string jwtAudience = configuration["Authentication:Jwt:Audience"]
                ?? throw new InvalidOperationException("Authentication:Jwt:Audience is missing.");

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddCookie(ExternalCookiesScheme, options =>
                {
                    options.Cookie.Name = "ExternalAuth.Cookie";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
                            context.NoResult();
                            context.HttpContext.Items[JwtAuthenticationExceptionItemKey] = context.Exception;
                            return Task.CompletedTask;
                        },
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            if (context.Response.HasStarted)
                            {
                                return;
                            }

                            string message = "Access denied. You must be logged in.";

                            if (context.HttpContext.Items.TryGetValue(JwtAuthenticationExceptionItemKey, out object? authException))
                            {
                                if (authException is SecurityTokenExpiredException)
                                {
                                    message = "Token expired. Please log in again.";
                                }
                                else
                                {
                                    message = "Authentication failed.";
                                }
                            }

                            await WriteJsonAsync(
                                context.Response,
                                StatusCodes.Status401Unauthorized,
                                message);
                        },
                        OnForbidden = async context =>
                        {
                            if (context.Response.HasStarted)
                            {
                                return;
                            }

                            await WriteJsonAsync(
                                context.Response,
                                StatusCodes.Status403Forbidden,
                                "You do not have permission to access this resource.");
                        }
                    };
                })
                .AddFacebook("Facebook", options =>
                {
                    options.SignInScheme = ExternalCookiesScheme;
                    options.AppId = configuration["Authentication:Facebook:AppId"];
                    options.AppSecret = configuration["Authentication:Facebook:AppSecret"];
                    options.CallbackPath = new PathString("/login/auth/facebook-response");
                });
        }

        private static async Task ConfigureApplication(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseIpRateLimiting();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors(CorsPolicyName);
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            await InitializeMongoDbAsync(app);
        }

        private static void ConfigureMongoDb(IServiceCollection services, IMongoDbSettings settings)
        {
            services.AddSingleton<IMongoDatabase>(_ =>
            {
                MongoClient client = new MongoClient(settings.Url);
                return client.GetDatabase(settings.DatabaseName);
            });
        }

        private static async Task InitializeMongoDbAsync(IHost app)
        {
            using IServiceScope scope = app.Services.CreateScope();

            IMongoDatabase database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
            IMongoDbSettings mongoDbSettings = scope.ServiceProvider.GetRequiredService<IMongoDbSettings>();
            ISearchIndexService searchIndexService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();

            await MongoDbInitializer.InitializeCollectionsAsync(
                database,
                mongoDbSettings,
                searchIndexService);
        }

        private static void ValidateJwtSettings(IJwtSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Key))
            {
                throw new InvalidOperationException("JWT Key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.Issuer))
            {
                throw new InvalidOperationException("JWT Issuer is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.Audience))
            {
                throw new InvalidOperationException("JWT Audience is not configured.");
            }
        }

        private static void ValidateMongoDbSettings(IMongoDbSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Url))
            {
                throw new InvalidOperationException("MongoDB Url is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.DatabaseName))
            {
                throw new InvalidOperationException("MongoDB DatabaseName is not configured.");
            }
        }

        private static void ValidateMinIoSettings(IImageStorageSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Endpoint))
            {
                throw new InvalidOperationException("MinIO Endpoint is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.AccessKey))
            {
                throw new InvalidOperationException("MinIO AccessKey is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.SecretKey))
            {
                throw new InvalidOperationException("MinIO SecretKey is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.WithSsl))
            {
                throw new InvalidOperationException("MinIO WithSsl is not configured.");
            }
        }

        private static TSettings GetRequiredSettings<TSettings>(IConfiguration configuration, string sectionPath)
            where TSettings : class, new()
        {
            IConfigurationSection section = configuration.GetSection(sectionPath);

            if (!section.Exists())
            {
                throw new InvalidOperationException($"Configuration section '{sectionPath}' is missing.");
            }

            TSettings? settings = section.Get<TSettings>();

            if (settings is null)
            {
                throw new InvalidOperationException($"Configuration section '{sectionPath}' is invalid.");
            }

            return settings;
        }

        private static Task WriteJsonAsync(HttpResponse response, int statusCode, string message)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";

            string payload = JsonSerializer.Serialize(new
            {
                message
            });

            return response.WriteAsync(payload);
        }
    }
}