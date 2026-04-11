namespace AmusementPark.WebAPI.Diagnostics;

/// <summary>
/// Centralise les informations de diagnostic exposées via l'endpoint <c>/health</c>
/// pour piloter la nouvelle roadmap de migration.
/// </summary>
public static class MigrationDiagnostics
{
    public static object CreateHealthPayload()
    {
        return new
        {
            status = "ok",
            project = "AmusementPark.WebAPI",
            diagnostics = new
            {
                transport = "health",
                architectureEndpoints = "removed",
            },
            migration = new
            {
                roadmap = "front-back-roadmap-v3-detailed-phases",
                status = "in-progress",
                currentPhase = "P01",
                currentPhaseTitle = "Durcissement back — exposition des endpoints Users",
                nextPhase = "P02",
                referenceDocument = "API/ARCHITECTURE/P01-USERS-ENDPOINT-HARDENING.md",
                principles = new[]
                {
                    "Iso-fonctionnel obligatoire",
                    "Une responsabilité claire par phase",
                    "Découpage avant optimisation",
                    "Contrats HTTP pilotés explicitement",
                    "Pas de refactor opportuniste hors périmètre",
                },
                completionChecklist = new[]
                {
                    "Build OK",
                    "Pas de rupture de navigation principale",
                    "Pas de changement fonctionnel non prévu",
                    "Diff cohérente avec l'objectif de la phase",
                    "Pas de nouvelle dépendance architecturale illégitime",
                },
                phases = new[]
                {
                    new { code = "P00", title = "Cadre de migration et gel des conventions", state = "done" },
                    new { code = "P01", title = "Durcissement back — exposition des endpoints Users", state = "current" },
                    new { code = "P02", title = "Fondation de l'architecture front — structure cible", state = "planned" },
                    new { code = "P03", title = "Fondation de l'architecture front — contrats transverses", state = "planned" },
                    new { code = "P04", title = "Refactor du socle HTTP front — extraction Auth API", state = "planned" },
                    new { code = "P05", title = "Refactor du socle HTTP front — extraction des API domain services", state = "planned" },
                    new { code = "P06", title = "Back — vraie refonte du refresh token", state = "planned" },
                    new { code = "P07", title = "Évolution coordonnée — bascule vers cookie HttpOnly", state = "planned" },
                    new { code = "P08", title = "Front — stratégie d'état Angular 21", state = "planned" },
                    new { code = "P09", title = "Front — refactor shared et core transverses", state = "planned" },
                    new { code = "P10", title = "Front — refonte clean archi de la feature Parks", state = "planned" },
                    new { code = "P11", title = "Front — refonte clean archi de la feature Park Items", state = "planned" },
                    new { code = "P12", title = "Front — refonte clean archi Admin Parks", state = "planned" },
                    new { code = "P13", title = "Front — refonte clean archi Admin Park Items", state = "planned" },
                    new { code = "P14", title = "Front — refonte clean archi Admin Data / imports", state = "planned" },
                    new { code = "P15", title = "Back — découpage final des gros blocs Infrastructure", state = "planned" },
                    new { code = "P16", title = "Sécurité front ciblée", state = "planned" },
                    new { code = "P17", title = "Back — hygiène finale et cohérence transverse", state = "planned" },
                    new { code = "P18", title = "Finition front — dette résiduelle et cohérence finale", state = "planned" },
                },
            },
            currentArchitecture = new
            {
                core = AmusementPark.Core.ArchitecturePhase.Current,
                application = AmusementPark.Application.ArchitecturePhase.Current,
                infrastructure = AmusementPark.Infrastructure.ArchitecturePhase.Current,
            },
            migratedApiFeatures = new[]
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
        };
    }
}
