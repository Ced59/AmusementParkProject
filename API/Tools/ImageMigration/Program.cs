using MongoDB.Bson;
using MongoDB.Driver;

if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run --project API/Tools/ImageMigration/ImageMigration.csproj -- <mongo-connection-string> <database-name>");
    return;
}

string connectionString = args[0];
string databaseName = args[1];

MongoClient client = new(connectionString);
IMongoDatabase database = client.GetDatabase(databaseName);
IMongoCollection<BsonDocument> images = database.GetCollection<BsonDocument>("images");
IMongoCollection<BsonDocument> imageTags = database.GetCollection<BsonDocument>("imageTags");

List<BsonDocument> legacyImages = await images.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
Dictionary<string, string> tagIdsBySlug = new(StringComparer.OrdinalIgnoreCase);

foreach (BsonDocument image in legacyImages)
{
    List<string> newTagIds = new();

    if (image.TryGetValue("tags", out BsonValue tagsValue) && tagsValue.IsBsonArray)
    {
        foreach (BsonValue rawTag in tagsValue.AsBsonArray)
        {
            string slug = rawTag.AsString.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            if (!tagIdsBySlug.TryGetValue(slug, out string? tagId))
            {
                BsonDocument? existingTag = await imageTags.Find(Builders<BsonDocument>.Filter.Eq("slug", slug)).FirstOrDefaultAsync();
                if (existingTag == null)
                {
                    tagId = Guid.NewGuid().ToString("N");
                    BsonDocument tag = new()
                    {
                        { "_id", tagId },
                        { "slug", slug },
                        { "labels", new BsonArray { new BsonDocument { { "languageCode", "fr" }, { "value", slug } } } },
                        { "descriptions", new BsonArray() },
                        { "isActive", true },
                        { "createdAt", DateTime.UtcNow },
                        { "updatedAt", DateTime.UtcNow }
                    };

                    await imageTags.InsertOneAsync(tag);
                }
                else
                {
                    tagId = existingTag["_id"].AsString;
                }

                tagIdsBySlug[slug] = tagId;
            }

            newTagIds.Add(tagIdsBySlug[slug]);
        }
    }

    if (image.TryGetValue("description", out BsonValue descriptionValue) && descriptionValue.IsString && !image.Contains("captions"))
    {
        image["captions"] = new BsonArray
        {
            new BsonDocument
            {
                { "languageCode", "fr" },
                { "value", descriptionValue.AsString }
            }
        };
    }

    if (image.TryGetValue("latitude", out BsonValue latitudeValue) && image.TryGetValue("longitude", out BsonValue longitudeValue))
    {
        double latitude = latitudeValue.ToDouble();
        double longitude = longitudeValue.ToDouble();
        if (!(latitude == 0d && longitude == 0d))
        {
            image["geoLocation"] = new BsonDocument
            {
                { "latitude", latitude },
                { "longitude", longitude }
            };
        }

        image.Remove("latitude");
        image.Remove("longitude");
        image.Remove("location");
    }

    image["tagIds"] = new BsonArray(newTagIds.Distinct());
    image["altTexts"] = image.Contains("altTexts") ? image["altTexts"] : new BsonArray();
    image["captions"] = image.Contains("captions") ? image["captions"] : new BsonArray();
    image["credits"] = image.Contains("credits") ? image["credits"] : new BsonArray();
    image["width"] = image.Contains("width") ? image["width"] : 0;
    image["height"] = image.Contains("height") ? image["height"] : 0;
    image["sizeInBytes"] = image.Contains("sizeInBytes") ? image["sizeInBytes"] : 0L;
    image["isPublished"] = image.Contains("isPublished") ? image["isPublished"] : true;

    await images.ReplaceOneAsync(Builders<BsonDocument>.Filter.Eq("_id", image["_id"].AsString), image);
}

Console.WriteLine($"Migrated {legacyImages.Count} images.");
