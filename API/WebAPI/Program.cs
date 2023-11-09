using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Liste des cultures supportées
            var supportedCultures = new List<CultureInfo> { new("en-US"), new("fr-FR"), new("de-DE"), new("es-ES") };

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Ajout des ressources statiques de traduction
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            // Ajout du service de localisation
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
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

            // MiddleWare pour sélectionner automatiquement la culture en fonction de la langue du navigateur ou de la langue selectionnée
            app.UseRequestLocalization(options =>
            {
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                options.ApplyCurrentCultureToResponseHeaders = true; // Ajout de l'entête Content-Language avec la culture par défaut du navigateur

                options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider()); // Culture par query string
                options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider()); // Culture par cookie

                options.DefaultRequestCulture = new RequestCulture("en-US"); // Culture par défaut
            });

            app.Run();
        }
    }
}