namespace Dtos.Parks.Logos;

public sealed class ParkLogoCreateDto
{
    /// <summary>
    /// Id de l'image déjà créée (dans ta collection Images / MinIO)
    /// </summary>
    public required string ImageId { get; set; }

    public string? Description { get; set; }
}