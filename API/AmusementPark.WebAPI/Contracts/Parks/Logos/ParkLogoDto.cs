namespace AmusementPark.WebAPI.Contracts.Parks.Logos;

/// <summary>
/// Contrat HTTP de lecture d'un logo de parc.
/// </summary>
public sealed class ParkLogoDto
{
    public string Id { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string ImageId { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCurrent { get; set; }

    public DateTime CreatedAt { get; set; }
}
