using Entities.Model.Parks;
using Entities.Model.Searching;
using MongoDB.Driver;

namespace Services.Interfaces.Searching
{
    /// <summary>
    /// Service pour peupler / maintenir la collection SearchItems.
    /// </summary>
    public interface ISearchIndexService
    {
        /// <summary>
        /// Initialise entièrement la collection SearchItems à partir des Parks existants.
        /// Utilisé par l’Initializer au démarrage.
        /// </summary>
        Task InitializeFromParksAsync(IMongoDatabase database, string parksCollectionName, string searchItemCollectionName);

        /// <summary>
        /// Extrait un SearchItem depuis un Park existant (pour Insert/Update).
        /// </summary>
        SearchItem ConvertParkToSearchItem(Park park);

        /// <summary>
        /// Insère ou met à jour un SearchItem dans la collection (upsert).
        /// </summary>
        Task UpsertSearchItemAsync(SearchItem item, string searchItemCollectionName);

        /// <summary>
        /// Supprime un SearchItem d’après son originalId (ex. "park_...").
        /// </summary>
        Task DeleteSearchItemAsync(string originalId, string searchItemCollectionName);
    }
}