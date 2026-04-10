using AmusementPark.Application;
using AmusementPark.Application.Architecture;
using AmusementPark.Core.Domain;
using AmusementPark.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur minimal de vérification des paliers d'architecture.
/// </summary>
[ApiController]
[Route("architecture")]
public sealed class ArchitectureController : ControllerBase
{
    [HttpGet("phase-1")]
    public IActionResult GetPhase1Status()
    {
        return Ok(new
        {
            projects = new[]
            {
                "AmusementPark.Core",
                "AmusementPark.Application",
                "AmusementPark.Infrastructure",
                "AmusementPark.WebAPI",
            },
            dependencies = new[]
            {
                "Application -> Core",
                "Infrastructure -> Application + Core",
                "WebAPI -> Application + Infrastructure",
            },
        });
    }

    [HttpGet("phase-3")]
    public IActionResult GetPhase3Status()
    {
        return Ok(new
        {
            phase = 3,
            goal = "Core pur extrait",
            constraints = new[]
            {
                "No Mongo in Core",
                "No AspNetCore in Core",
                "No MinIO/MailKit/ImageSharp in Core",
            },
            extractedTypes = DomainCatalog.ExtractedTypes,
        });
    }

    [HttpGet("phase-4")]
    public IActionResult GetPhase4Status()
    {
        return Ok(new
        {
            phase = 4,
            label = AmusementPark.Application.ArchitecturePhase.Current,
            rules = ArchitectureRules.All,
            features = FeatureCatalog.All,
            useCases = UseCaseCatalog.ByFeature,
        });
    }

    [HttpGet("phase-5")]
    public IActionResult GetPhase5Status()
    {
        return Ok(new
        {
            phase = 5,
            label = AmusementPark.Infrastructure.ArchitecturePhase.Current,
            documents = new[]
            {
                "CountryDocument",
                "ParkDocument",
                "ParkZoneDocument",
                "ParkItemDocument",
                "ParkFounderDocument",
                "ParkOperatorDocument",
                "AttractionManufacturerDocument",
                "ImageDocument",
                "ImageTagDocument",
                "UserDocument",
                "SearchItemDocument",
                "CaptainCoasterSettingsDocument",
                "CaptainCoasterSyncSessionDocument",
            },
            adapters = new[]
            {
                "CountryReadRepository",
                "ParkFounderRepository",
                "ParkOperatorRepository",
                "AttractionManufacturerRepository",
                "ParkRepository",
                "ParkZoneRepository",
                "ParkItemRepository",
                "SearchReadRepository",
                "ImageRepository",
                "ImageTagRepository",
                "UserRepository",
                "CaptainCoasterSettingsRepository",
                "CaptainCoasterSessionRepository",
            },
        });
    }

    [HttpGet("phase-6")]
    public IActionResult GetPhase6Status()
    {
        return Ok(new
        {
            phase = 6,
            goal = "Features simples migrées de bout en bout",
            migratedFeatures = new[]
            {
                "Countries",
                "ParkFounders",
                "ParkOperators",
                "AttractionManufacturers",
            },
            preservedRoutes = new[]
            {
                "GET /Countries",
                "GET /park-founders",
                "GET /park-founders/{id}",
                "POST /park-founders",
                "PUT /park-founders/{id}",
                "GET /park-operators",
                "GET /park-operators/{id}",
                "POST /park-operators",
                "PUT /park-operators/{id}",
                "GET /attraction-manufacturers",
                "GET /attraction-manufacturers/{id}",
                "POST /attraction-manufacturers",
                "PUT /attraction-manufacturers/{id}",
            },
        });
    }

    [HttpGet("phase-7")]
    public IActionResult GetPhase7Status()
    {
        return Ok(new
        {
            phase = 7,
            goal = "Migration Parks et ParkZones de bout en bout",
            migratedFeatures = new[]
            {
                "Countries",
                "ParkFounders",
                "ParkOperators",
                "AttractionManufacturers",
                "Parks",
                "ParkZones",
            },
            preservedRoutes = new[]
            {
                "POST /Parks",
                "GET /Parks/{id}",
                "GET /Parks?page=&size=&name=",
                "GET /Parks/geo-search?latitude=&longitude=&radius=",
                "PATCH /Parks/{id}/visibility",
                "PUT /Parks/{id}",
                "GET /park-zones/park/{parkId}",
                "GET /park-zones/{id}",
                "GET /park-zones/park/{parkId}/explorer",
                "POST /park-zones",
                "PUT /park-zones/{id}",
                "DELETE /park-zones/{id}",
            },
            notes = new[]
            {
                "currentLogoImageId conservé dans le contrat Park",
                "explorer ParkZones restitué au format legacy avec overview/zones/unassigned",
                "projection search parks et parkItems rafraîchie lors des mutations de visibilité ou de contenu des parcs",
            },
        });
    }

    [HttpGet("phase-10")]
    public IActionResult GetPhase10Status()
    {
        return Ok(new
        {
            phase = 10,
            goal = "Migration Users / Auth / Email / Avatar",
            migratedFeatures = new[]
            {
                "Users",
                "Auth",
            },
            preservedRoutes = new[]
            {
                "POST /auth/login",
                "POST /auth/refresh-token",
                "POST /auth/external/{provider}",
                "GET /auth/facebook",
                "GET /auth/facebook-response",
                "POST /users",
                "GET /users/by-email?email=",
                "GET /users/{id}",
                "GET /users?page=&size=",
                "PUT /users/{id}",
                "POST /users/change-password?idUser=",
                "POST /users/confirm-email",
                "POST /users/resend-confirmation",
                "POST /users/forgot-password",
                "POST /users/reset-password",
                "POST /users/roles/assign/{userId}",
                "DELETE /users/roles/remove/{userId}",
                "POST /users/lock",
                "POST /users/unlock",
            },
            notes = new[]
            {
                "UsersService éclaté en handlers Application dédiés",
                "JWT, hashage, email et avatar externe déplacés derrière des ports Infrastructure",
                "Routes et payloads HTTP legacy conservés côté WebAPI",
            },
        });
    }
}
