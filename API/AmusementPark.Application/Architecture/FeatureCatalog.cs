namespace AmusementPark.Application.Architecture
{
    /// <summary>
    /// Catalogue central des feature slices à migrer.
    /// </summary>
    public static class FeatureCatalog
    {
        /// <summary>
        /// Obtient la liste ordonnée des feature slices de la refonte.
        /// </summary>
        public static IReadOnlyList<FeatureSlice> All { get; } = new List<FeatureSlice>
        {
            new FeatureSlice("Countries", 1, "Référentiel pays utilisé par les cas d'usage publics et d'administration."),
            new FeatureSlice("ParkFounders", 2, "Fondateurs de parc et référentiel d'administration."),
            new FeatureSlice("ParkOperators", 3, "Exploitants de parc et référentiel d'administration."),
            new FeatureSlice("AttractionManufacturers", 4, "Constructeurs d'attractions et référentiel d'administration."),
            new FeatureSlice("Users", 5, "Comptes, authentification, rôles, profil, email et avatar."),
            new FeatureSlice("Parks", 6, "Parcs, visibilité, lecture et écriture des agrégats principaux."),
            new FeatureSlice("ParkZones", 7, "Zones de parc et rattachements au parc."),
            new FeatureSlice("ParkItems", 8, "Attractions et autres park items avec leurs détails métiers."),
            new FeatureSlice("Images", 9, "Images, liens d'ownership, tags, compression, watermark et métadonnées."),
            new FeatureSlice("Videos", 10, "Videos externes, enrichissement de metadonnees, tags et liens d'ownership."),
            new FeatureSlice("Search", 11, "Projection technique pour la recherche et l'indexation."),
            new FeatureSlice("DataSources", 12, "Socle générique d'ingestion et de synchronisation de sources externes, avec Captain Coaster comme premier provider."),
            new FeatureSlice("AdminAudit", 13, "Journalisation et consultation des actions d'administration sensibles."),
            new FeatureSlice("Ratings", 14, "Notes utilisateurs authentifies, agregats publics et classements."),
        };
    }
}
