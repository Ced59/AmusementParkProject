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

            // Services
            builder.Services.AddScoped<IUsersService, UsersService>();
            builder.Services.AddScoped<IParksService, ParksService>();
            builder.Services.AddScoped<IParkFoundersService, ParkFoundersService>();
            builder.Services.AddScoped<IParkOperatorsService, ParkOperatorsService>();

            builder.Services.AddScoped<ISocialAuthService, SocialAuthService>();

            builder.Services.AddScoped<ISearchIndexService, SearchIndexService>();
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.Services.AddScoped<ICountriesService, CountriesService>();

            builder.Services.AddScoped<IUserQueryHandler, UsersMongoQueryHandler>();
            builder.Services.AddScoped<IParksQueryHandler, ParksMongoQueryHandler>();
            builder.Services.AddScoped<IParkFoundersQueryHandler, ParkFoundersMongoQueryHandler>();
            builder.Services.AddScoped<IParkOperatorsQueryHandler, ParkOperatorsMongoQueryHandler>();
            builder.Services.AddScoped<ISearchQueryHandler, SearchMongoQueryHandler>();
            builder.Services.AddScoped<IImagesQueryHandler, ImagesMongoQueryHandler>();
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
                options.AddPolicy("AllowSpecificOrigin",
                    builderCors => builderCors.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
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
            services.AddAuthentication(options =>
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
                .AddFacebook("Facebook", options =>
                {
                    options.SignInScheme = "ExternalCookies";
                    options.AppId = configuration["Authentication:Facebook:AppId"];
                    options.AppSecret = configuration["Authentication:Facebook:AppSecret"];
                    options.CallbackPath = new PathString("/login/auth/facebook-response");
                });
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
            // Cr�e un scope pour r�soudre ISearchIndexService (scoped)
            using IServiceScope scope = app.Services.CreateScope();

            IMongoDatabase database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
            IMongoDbSettings mongoDbSettings = scope.ServiceProvider.GetRequiredService<IMongoDbSettings>();
            ISearchIndexService searchItemService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();

            await MongoDbInitializer.InitializeCollectionsAsync(
                database,
                mongoDbSettings,
                searchItemService
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