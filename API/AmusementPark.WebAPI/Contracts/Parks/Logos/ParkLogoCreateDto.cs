namespace AmusementPark.WebAPI.Contracts.Parks.Logos;

/// <summary>
/// Contrat HTTP de création d'un logo de parc à partir d'une image existante.
/// </summary>
public sealed class ParkLogoCreateDto
{
    public required string ImageId { get; set; }

    public string? Description { get; set; }
}
