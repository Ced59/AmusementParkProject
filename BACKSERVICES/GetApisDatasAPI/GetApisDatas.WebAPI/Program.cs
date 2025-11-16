
using System.Reflection;
using GetApisDatas.WebAPI.Settings;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GetApisDatas.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddHttpClient();

            ConfigureSwagger(builder.Services);

            builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Get Apis Datas Web API", Version = "v1" });
                c.OrderActionsBy(apiDesc =>
                {
                    SwaggerOrderAttribute? orderAttr = apiDesc.CustomAttributes().OfType<SwaggerOrderAttribute>().FirstOrDefault();
                    return orderAttr != null ? orderAttr.Order.ToString() : int.MaxValue.ToString();
                });

                string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

        }
    }
}
