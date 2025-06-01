using Entities.Model.Parks;
using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Text.Encodings.Web;
using System.Text.Json;
using WebAPI.Resources.InitializingDatas;

namespace WebAPI.Settings.MongoDB;

public static class MongoDbInitializer
{
    public static async Task InitializeCollectionsAsync(IMongoDatabase database, IMongoDbSettings settings, ISearchIndexService searchIndexService)
    {
        await EnsureCollectionExistsAsync(database, settings.UsersCollectionName);
        await InitializeParksCollection(database, settings.ParksCollectionName, @"C:\Users\ccaud\Source\Repos\Ced59\AmusementParkProject\API\WebAPI\Resources\InitializingDatas\parks.json");
        await searchIndexService.InitializeFromParksAsync(
            database, 
            settings.ParksCollectionName,
            settings.SearchItemCollectionName
            );
    }

    private static async Task EnsureCollectionExistsAsync(IMongoDatabase database, string collectionName)
    {
        var filter = new BsonDocument("name", collectionName);
        var collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
        var exists = await collections.AnyAsync();

        if (!exists) await database.CreateCollectionAsync(collectionName);
    }

    private static async Task InitializeParksCollection(IMongoDatabase database, string collectionName, string jsonFilePath)
    {
        var filter = new BsonDocument("name", collectionName);
        var collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
        var exists = await collections.AnyAsync();

        if (!exists)
        {
            await database.CreateCollectionAsync(collectionName);
        }

        var parksCollection = database.GetCollection<Park>(collectionName);

        var documentCount = await parksCollection.CountDocumentsAsync(new BsonDocument());

        if (documentCount == 0)
        {
            // Lire le fichier JSON et déséchapper les caractères spéciaux
            var json = await File.ReadAllTextAsync(jsonFilePath);

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNameCaseInsensitive = true
            };

            var parksJson = JsonSerializer.Deserialize<List<ParkJson>>(json, options);

            // Création de l'index de géolocalisation sur la collection Park
            parksCollection.Indexes.CreateOne(new CreateIndexModel<Park>(
                Builders<Park>.IndexKeys.Geo2DSphere(park => park.Location)));

            var parks = new List<Park>();

            foreach (var parkJson in parksJson)
            {
                var park = new Park
                {
                    Name = parkJson.Name,
                    CountryCode = ExtractCountryCode(parkJson.Country.Name),
                    Latitude = parkJson.Latitude,
                    Longitude = parkJson.Longitude
                };

                parks.Add(park);
            }

            if (parks is { Count: > 0 })
            {
                // Insérer les parcs dans la collection MongoDB
                await parksCollection.InsertManyAsync(parks);
            }
        }
    }

    private static string ExtractCountryCode(string? countryName)
    {
        if (string.IsNullOrEmpty(countryName) || !CountryToCode.TryGetValue(countryName, out var code))
        {
            return "Unknown"; // Code par défaut si le pays n'est pas trouvé ou si la chaîne est vide
        }
        return code;
    }


    private static readonly Dictionary<string, string> CountryToCode = new Dictionary<string, string>
    {
        // List of countries without prefix
        {"Albania", "AL"},
        {"Andorra", "AD"},
        {"Armenia", "AM"},
        {"Belarus", "BY"},
        {"Bosnia and Herzegovina", "BA"},
        {"Bulgaria", "BG"},
        {"Croatia", "HR"},
        {"Egypt", "EG"},
        {"Estonia", "EE"},
        {"Georgia", "GE"},
        {"Greece", "GR"},
        {"Haiti", "HT"},
        {"Iran", "IR"},
        {"Jamaica", "JM"},
        {"Kazakhstan", "KZ"},
        {"Kosovo", "XK"},
        {"Latvia", "LV"},
        {"Lithuania", "LT"},
        {"Luxembourg", "LU"},
        {"Malta", "MT"},
        {"Moldova", "MD"},
        {"Montenegro", "ME"},
        {"North Korea", "KP"},
        {"North Macedonia", "MK"},
        {"Oman", "OM"},
        {"Pakistan", "PK"},
        {"Palestine", "PS"},
        {"Philippines", "PH"},
        {"Romania", "RO"},
        {"Saudi Arabia", "SA"},
        {"Serbia", "RS"},
        {"Slovakia", "SK"},
        {"Slovenia", "SI"},
        {"Sri Lanka", "LK"},
        {"Tunisia", "TN"},
        {"Uzbekistan", "UZ"},

        // List of countries with "country." prefix
        {"country.argentina", "AR"},
        {"country.australia", "AU"},
        {"country.austria", "AT"},
        {"country.belgium", "BE"},
        {"country.brazil", "BR"},
        {"country.burma", "MM"},
        {"country.canada", "CA"},
        {"country.china", "CN"},
        {"country.colombia", "CO"},
        {"country.cyprus", "CY"},
        {"country.czech", "CZ"},
        {"country.denmark", "DK"},
        {"country.finland", "FI"},
        {"country.france", "FR"},
        {"country.germany", "DE"},
        {"country.guatemala", "GT"},
        {"country.hungary", "HU"},
        {"country.india", "IN"},
        {"country.indonesia", "ID"},
        {"country.iraq", "IQ"},
        {"country.ireland", "IE"},
        {"country.israel", "IL"},
        {"country.italy", "IT"},
        {"country.japan", "JP"},
        {"country.lebanon", "LB"},
        {"country.malaysia", "MY"},
        {"country.mexico", "MX"},
        {"country.mongolia", "MN"},
        {"country.na", "N/A"},  // Placeholder for "N/A", replace as needed
        {"country.netherlands", "NL"},
        {"country.newzealand", "NZ"},
        {"country.norway", "NO"},
        {"country.peru", "PE"},
        {"country.poland", "PL"},
        {"country.portugal", "PT"},
        {"country.qatar", "QA"},
        {"country.russia", "RU"},
        {"country.singapore", "SG"},
        {"country.southafrica", "ZA"},
        {"country.southkorea", "KR"},
        {"country.spain", "ES"},
        {"country.sweden", "SE"},
        {"country.switzerland", "CH"},
        {"country.taiwan", "TW"},
        {"country.thailand", "TH"},
        {"country.turkey", "TR"},
        {"country.uae", "AE"},
        {"country.uk", "GB"},
        {"country.ukraine", "UA"},
        {"country.usa", "US"},
        {"country.vietnam", "VN"}
    };
}
