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
            new FeatureSlice("Users", 1, "Comptes, authentification, rôles, profil, email et avatar."),
            new FeatureSlice("Parks", 2, "Parcs, visibilité, lecture et écriture des agrégats principaux."),
            new FeatureSlice("ParkZones", 3, "Zones de parc et rattachements au parc."),
            new FeatureSlice("ParkItems", 4, "Attractions et autres park items avec leurs détails métiers."),
            new FeatureSlice("Images", 5, "Images, liens d'ownership, tags, compression, watermark et métadonnées."),
            new FeatureSlice("Search", 6, "Projection technique pour la recherche et l'indexation."),
            new FeatureSlice("CaptainCoaster", 7, "Import, comparaison, résolution de doublons et application des changements."),
        };
    }
}
