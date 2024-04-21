using System.Globalization;
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

namespace WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            // Configuration de Swagger
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Amusement Parks Web API", Version = "v1" });

                c.OrderActionsBy((apiDesc) =>
                {
                    var orderAttr = apiDesc.CustomAttributes().OfType<SwaggerOrderAttribute>().FirstOrDefault();
                    return orderAttr != null ? orderAttr.Order.ToString() : int.MaxValue.ToString();
                });
            });

            // Configuration de l'authentification OAuth avec Google et Facebook
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Application";
                    options.DefaultChallengeScheme = "Google";
                })
                .AddGoogle(options =>
                {
                    options.ClientId = "<Google-Client-ID>";
                    options.ClientSecret = "<Google-Client-Secret>";
                })
                .AddFacebook(options =>
                {
                    options.AppId = "<Facebook-App-ID>";
                    options.AppSecret = "<Facebook-App-Secret>";
                });



            // Injection des services
            builder.Services.AddScoped<IUsersService, UsersService>();


            // Services MongoDB
            var mongoDbSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
            builder.Services.AddSingleton<IMongoDbSettings>(mongoDbSettings);

            builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
            {
                var settings = serviceProvider.GetRequiredService<IMongoDbSettings>();
                var client = new MongoClient(settings.Url);
                return client.GetDatabase(settings.DatabaseName);
            });

            builder.Services.AddScoped<IUserQueryHandler, UsersMongoQueryHandler>();

            // Convertisseur d'enum
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });




            // Ajout de la prise en charge des routes en minuscule
            builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            var mongoDatabase = app.Services.GetRequiredService<IMongoDatabase>();
            MongoDbInitializer.InitializeCollections(mongoDatabase, mongoDbSettings);


            app.Run();
        }
    }
}