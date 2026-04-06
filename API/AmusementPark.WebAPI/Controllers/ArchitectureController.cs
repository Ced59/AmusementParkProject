using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur minimal de vérification du squelette phase 1.
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
                "AmusementPark.WebAPI"
            },
            dependencies = new[]
            {
                "Application -> Core",
                "Infrastructure -> Application + Core",
                "WebAPI -> Application + Infrastructure"
            }
        });
    }
}
