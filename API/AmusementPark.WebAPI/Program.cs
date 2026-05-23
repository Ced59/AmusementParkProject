using AmusementPark.Infrastructure.DependencyInjection;
using AmusementPark.WebAPI.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationModules();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMongoInitialization();
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddApiSwagger();
builder.Services.AddHttpApi();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

WebApplication app = builder.Build();

await app.InitializeMongoAsync();

if (app.Environment.IsDevelopment())
{
    app.UseApiSwagger();
}

app.UseApiPipeline();
app.MapDiagnosticEndpoints();

app.Run();
