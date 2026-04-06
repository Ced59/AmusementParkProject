namespace AmusementPark.Application.Architecture
{
    /// <summary>
    /// Représente une feature slice cible de la refonte.
    /// </summary>
    /// <param name="Name">Nom logique de la feature.</param>
    /// <param name="MigrationPriority">Ordre recommandé de migration.</param>
    /// <param name="Description">Description synthétique du périmètre.</param>
    public sealed record FeatureSlice(string Name, int MigrationPriority, string Description);
}
