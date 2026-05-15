using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Initialization;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Initialise les collections MongoDB métier, leurs index et les seeds de base.
/// </summary>
public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeParksIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkDocument> parksCollection = this.database.GetCollection<ParkDocument>(this.settings.ParksCollectionName);
        List<CreateIndexModel<ParkDocument>> indexes = new List<CreateIndexModel<ParkDocument>>
        {
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_parks_name" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.CountryCode),
                new CreateIndexOptions { Name = "idx_parks_country_code" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_parks_visibility_updated" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.IsVisible).Ascending(item => item.Name).Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_visibility_name_id" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "idx_parks_location" }),
        };

        await parksCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkFoundersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkFounderDocument> collection = this.database.GetCollection<ParkFounderDocument>(this.settings.ParkFoundersCollectionName);
        List<CreateIndexModel<ParkFounderDocument>> indexes = new List<CreateIndexModel<ParkFounderDocument>>
        {
            new CreateIndexModel<ParkFounderDocument>(
                Builders<ParkFounderDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_founders_name_unique", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkOperatorsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkOperatorDocument> collection = this.database.GetCollection<ParkOperatorDocument>(this.settings.ParkOperatorsCollectionName);
        List<CreateIndexModel<ParkOperatorDocument>> indexes = new List<CreateIndexModel<ParkOperatorDocument>>
        {
            new CreateIndexModel<ParkOperatorDocument>(
                Builders<ParkOperatorDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_operators_name_unique", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeAttractionManufacturersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<AttractionManufacturerDocument> collection = this.database.GetCollection<AttractionManufacturerDocument>(this.settings.AttractionManufacturersCollectionName);
        List<CreateIndexModel<AttractionManufacturerDocument>> indexes = new List<CreateIndexModel<AttractionManufacturerDocument>>
        {
            new CreateIndexModel<AttractionManufacturerDocument>(
                Builders<AttractionManufacturerDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_attraction_manufacturers_name_unique", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkZonesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkZoneDocument> collection = this.database.GetCollection<ParkZoneDocument>(this.settings.ParkZonesCollectionName);
        List<CreateIndexModel<ParkZoneDocument>> indexes = new List<CreateIndexModel<ParkZoneDocument>>
        {
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_zones_park_name" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_park_zones_park_slug" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.SortOrder)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_park_zones_park_sort_name" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "idx_park_zones_location" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkItemsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkItemDocument> collection = this.database.GetCollection<ParkItemDocument>(this.settings.ParkItemsCollectionName);
        List<CreateIndexModel<ParkItemDocument>> indexes = new List<CreateIndexModel<ParkItemDocument>>
        {
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_items_park_name" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.ZoneId),
                new CreateIndexOptions { Name = "idx_park_items_park_zone" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.Category)
                    .Ascending(item => item.Type)
                    .Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_items_park_category_type_name" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.ZoneId),
                new CreateIndexOptions { Name = "idx_park_items_zone_id" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.Category).Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_park_items_category_visibility_updated" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.Category).Ascending("attractionDetails.manufacturerId"),
                new CreateIndexOptions { Name = "idx_park_items_attraction_manufacturer" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "idx_park_items_location" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeImagesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ImageDocument> collection = this.database.GetCollection<ImageDocument>(this.settings.ImagesCollectionName);
        List<CreateIndexModel<ImageDocument>> indexes = new List<CreateIndexModel<ImageDocument>>
        {
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId).Ascending(item => item.Category).Ascending(item => item.IsCurrent),
                new CreateIndexOptions { Name = "idx_images_owner_category_current" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId),
                new CreateIndexOptions { Name = "idx_images_owner" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId).Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_owner_created_at_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Category)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_owner_category_created_at_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.Category),
                new CreateIndexOptions { Name = "idx_images_category" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_created_at" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending("tagIds"),
                new CreateIndexOptions { Name = "idx_images_tag_ids" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeImageTagsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ImageTagDocument> collection = this.database.GetCollection<ImageTagDocument>(this.settings.ImageTagsCollectionName);
        List<CreateIndexModel<ImageTagDocument>> indexes = new List<CreateIndexModel<ImageTagDocument>>
        {
            new CreateIndexModel<ImageTagDocument>(
                Builders<ImageTagDocument>.IndexKeys.Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_image_tags_slug_unique", Unique = true }),
            new CreateIndexModel<ImageTagDocument>(
                Builders<ImageTagDocument>.IndexKeys.Ascending(item => item.IsActive),
                new CreateIndexOptions { Name = "idx_image_tags_is_active" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
