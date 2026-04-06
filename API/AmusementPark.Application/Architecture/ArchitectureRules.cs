namespace AmusementPark.Application.Architecture
{
    /// <summary>
    /// Expose les règles d'architecture figées à partir de la phase 2.
    /// </summary>
    public static class ArchitectureRules
    {
        /// <summary>
        /// Obtient la liste des règles de dépendances et de séparation des couches.
        /// </summary>
        public static IReadOnlyList<string> All { get; } = new List<string>
        {
            "Core ne dépend d'aucun projet métier et ne référence aucune technologie d'infrastructure.",
            "Application dépend uniquement de Core, expose des commandes/requêtes/handlers/ports et reste indépendante du HTTP et de MongoDB.",
            "Infrastructure implémente les ports applicatifs et contient les détails techniques.",
            "WebAPI adapte HTTP vers Application et ne parle pas directement à MongoDB ou aux handlers legacy.",
            "Les DTOs web restent dans WebAPI.",
            "Les documents BSON restent dans Infrastructure.",
            "Les résultats applicatifs sont indépendants des codes de statut HTTP.",
        };
    }
}
