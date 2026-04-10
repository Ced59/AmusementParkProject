namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Centralise le pipeline HTTP et les endpoints transverses de l'API.
/// </summary>
public static class WebApplicationPipelineExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseApiCors();
        app.UseApiRateLimiting();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static WebApplication MapDiagnosticEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            architecture = "clean-architecture-phase-13",
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
                "ParkItems",
                "Images",
                "Users",
                "Auth",
                "Search",
                "DataSources",
                "CaptainCoaster",
            },
        }));

        return app;
    }
}
