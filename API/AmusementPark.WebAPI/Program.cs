using AmusementPark.Application;
using AmusementPark.Application.DependencyInjection;
using AmusementPark.Infrastructure;
using AmusementPark.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
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
    architecture = "clean-architecture-phase-5",
    application = AmusementPark.Application.ArchitecturePhase.Current,
    infrastructure = AmusementPark.Infrastructure.ArchitecturePhase.Current,
    project = "AmusementPark.WebAPI",
}));

app.Run();
