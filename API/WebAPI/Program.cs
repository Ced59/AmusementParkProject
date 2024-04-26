using System;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureServices(builder);
            var app = builder.Build();
            ConfigureApplication(app);

            app.Run();
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
            ConfigureMongoDB(builder.Services, mongoDbSettings);

            // Services
            builder.Services.AddScoped<IUsersService, UsersService>();
            builder.Services.AddScoped<IUserQueryHandler, UsersMongoQueryHandler>();

            // Other Configurations
            builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Amusement Parks Web API", Version = "v1" });
                c.OrderActionsBy((apiDesc) =>
                {
                    var orderAttr = apiDesc.CustomAttributes().OfType<SwaggerOrderAttribute>().FirstOrDefault();
                    return orderAttr != null ? orderAttr.Order.ToString() : int.MaxValue.ToString();
                });
            });
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
                        ClockSkew = TimeSpan.Zero
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



        private static void ValidateJwtSettings(JwtSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Key) || string.IsNullOrEmpty(settings.Issuer) || string.IsNullOrEmpty(settings.Audience))
            {
                throw new InvalidOperationException("JWT settings are not properly configured.");
            }
        }

        private static void ConfigureMongoDB(IServiceCollection services, MongoDbSettings settings)
        {
            services.AddSingleton<IMongoDatabase>(serviceProvider =>
            {
                var client = new MongoClient(settings.Url);
                return client.GetDatabase(settings.DatabaseName);
            });
        }

        private static void ConfigureApplication(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
        }
    }
}
