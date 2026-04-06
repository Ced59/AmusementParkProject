using AmusementPark.Application;
using AmusementPark.Application.Architecture;
using AmusementPark.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur minimal de vérification des paliers d'architecture.
/// </summary>
[ApiController]
[Route("architecture")]
public sealed class ArchitectureController : ControllerBase
{
    /// <summary>
    /// Retourne un état simple de la structure cible à quatre projets.
    /// </summary>
    /// <returns>État de la structure cible.</returns>
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

    /// <summary>
    /// Retourne l'état de la phase 3 d'extraction du Core pur.
    /// </summary>
    /// <returns>État du domaine pur extrait.</returns>
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

    /// <summary>
    /// Retourne l'état de la phase 4 de structuration de la couche Application.
    /// </summary>
    /// <returns>État des use cases et ports applicatifs créés.</returns>
    [HttpGet("phase-4")]
    public IActionResult GetPhase4Status()
    {
        return Ok(new
        {
            phase = 4,
            label = ArchitecturePhase.Current,
            rules = ArchitectureRules.All,
            features = FeatureCatalog.All,
            useCases = UseCaseCatalog.ByFeature,
        });
    }
}
