using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Repositories.Implementations;
using Repositories.Interfaces;
using Services.Implementations;
using Services.Interfaces;
using WebAPI.Settings;

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
            builder.Services.AddSwaggerGen();
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