using System.Threading.Tasks;
using Entities.Model.Parks;
using Entities.Model.Searching;
using MongoDB.Driver;

namespace Services.Interfaces.Searching
{
    public interface ISearchIndexService
    {
        Task InitializeAsync(
            IMongoDatabase database,
            string parksCollectionName,
            string parkItemsCollectionName,
            string parkOperatorsCollectionName,
            string attractionManufacturersCollectionName,
            string searchItemCollectionName);

        SearchItem ConvertParkToSearchItem(Park park);
        SearchItem ConvertParkItemToSearchItem(ParkItem parkItem, string parkName);
        SearchItem ConvertParkOperatorToSearchItem(ParkOperator parkOperator);
        SearchItem ConvertAttractionManufacturerToSearchItem(AttractionManufacturer manufacturer);
        Task UpsertSearchItemAsync(SearchItem item, string searchItemCollectionName);
        Task DeleteSearchItemAsync(string originalId, string searchItemCollectionName);
    }
}
