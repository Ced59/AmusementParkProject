using Common.General;

namespace Entities.Model.Images
{
    public class Image : GeolocatedEntity
    {
        public ImageCategory Category { get; set; }

        /// <summary>
        /// Chemin logique de l'image dans le stockage.
        /// </summary>
        public string? Path { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Type de propriétaire métier de l'image.
        /// </summary>
        public ImageOwnerType OwnerType { get; set; } = ImageOwnerType.None;

        /// <summary>
        /// Id métier du propriétaire.
        /// </summary>
        public string? OwnerId { get; set; }

        /// <summary>
        /// Indique si l'image est l'image courante de ce owner pour cette catégorie.
        /// </summary>
        public bool IsCurrent { get; set; }

        public string? OriginalFileName { get; set; }

        public string? ContentType { get; set; }
    }
}