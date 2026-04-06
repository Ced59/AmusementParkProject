namespace AmusementPark.Infrastructure.Persistence.Mongo;

/// <summary>
/// Options techniques de connexion Mongo pour la couche Infrastructure.
/// </summary>
public sealed class MongoDbOptions
{
    /// <summary>
    /// Nom de la section de configuration dédiée.
    /// </summary>
    public const string SectionName = "MongoDb";

    /// <summary>
    /// Chaîne de connexion MongoDB.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Nom de la base de données.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;
}
