using AmusementPark.Application;
using AmusementPark.Application.DependencyInjection;
using AmusementPark.Infrastructure;
using AmusementPark.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddApplicationHandlers(static type =>
{
    string? namespaceName = type.Namespace;
    if (string.IsNullOrWhiteSpace(namespaceName))
    {
        return false;
    }

    return namespaceName.Contains(".Features.Countries.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.ParkFounders.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.ParkOperators.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.AttractionManufacturers.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.Parks.", StringComparison.Ordinal) ||
           namespaceName.Contains(".Features.ParkZones.", StringComparison.Ordinal);
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    architecture = "clean-architecture-phase-7",
    application = AmusementPark.Application.ArchitecturePhase.Current,
    infrastructure = AmusementPark.Infrastructure.ArchitecturePhase.Current,
    project = "AmusementPark.WebAPI",
    migratedFeatures = new[]
    {
        "Countries",
        "ParkFounders",
        "ParkOperators",
        "AttractionManufacturers",
        "Parks",
        "ParkZones",
    },
}));

app.Run();
