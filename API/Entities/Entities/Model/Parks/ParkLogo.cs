using Common.General;

namespace Entities.Model.Parks
{
    /// <summary>
    /// Historique des logos d'un parc.
    /// Chaque entrée correspond à une image donnée,
    /// avec un flag IsCurrent pour le logo actuellement utilisé.
    /// </summary>
    public class ParkLogo : ModelBase
    {
        /// <summary>
        /// Id du parc concerné (Park.Id)
        /// </summary>
        public string ParkId { get; set; } = default!;

        /// <summary>
        /// Id de l'image (dans ta collection Images / MinIO)
        /// </summary>
        public string ImageId { get; set; } = default!;

        /// <summary>
        /// Description optionnelle du logo (ex: "Logo 2024", "Ancien logo vintage", etc.)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Indique si ce logo est celui actuellement utilisé pour le parc.
        /// </summary>
        public bool IsCurrent { get; set; }
    }
}