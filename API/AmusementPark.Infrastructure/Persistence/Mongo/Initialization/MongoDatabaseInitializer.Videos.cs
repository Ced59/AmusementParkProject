using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeVideosIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<VideoDocument> collection = this.database.GetCollection<VideoDocument>(this.settings.VideosCollectionName);
        List<CreateIndexModel<VideoDocument>> indexes = new List<CreateIndexModel<VideoDocument>>
        {
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_owner_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Type)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_owner_type_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.HostingProvider)
                    .Ascending(item => item.ExternalId),
                new CreateIndexOptions { Name = "idx_videos_provider_external_id" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys.Ascending("tagIds"),
                new CreateIndexOptions { Name = "idx_videos_tag_ids" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys.Ascending("languageCodes"),
                new CreateIndexOptions { Name = "idx_videos_language_codes" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.CreatorName)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_creator_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Text(item => item.Title)
                    .Text(item => item.Description)
                    .Text(item => item.CreatorName)
                    .Text(item => item.CanonicalUrl)
                    .Text(item => item.ExternalId),
                new CreateIndexOptions { Name = "idx_videos_admin_text" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeVideoTagsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<VideoTagDocument> collection = this.database.GetCollection<VideoTagDocument>(this.settings.VideoTagsCollectionName);
        List<CreateIndexModel<VideoTagDocument>> indexes = new List<CreateIndexModel<VideoTagDocument>>
        {
            new CreateIndexModel<VideoTagDocument>(
                Builders<VideoTagDocument>.IndexKeys.Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_video_tags_slug_unique", Unique = true }),
            new CreateIndexModel<VideoTagDocument>(
                Builders<VideoTagDocument>.IndexKeys.Ascending(item => item.IsActive),
                new CreateIndexOptions { Name = "idx_video_tags_is_active" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task SeedSystemVideoTagsAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<VideoTagDocument> collection = this.database.GetCollection<VideoTagDocument>(this.settings.VideoTagsCollectionName);
        DateTime now = DateTime.UtcNow;
        IReadOnlyCollection<VideoTagDocument> tags = new List<VideoTagDocument>
        {
            CreateSystemVideoTag(
                "video-tag-official-amusementparks",
                "official-amusementparks",
                new Dictionary<string, string>
                {
                    ["fr"] = "Officiel AmusementParks.fun",
                    ["en"] = "Official AmusementParks.fun",
                    ["de"] = "Offiziell AmusementParks.fun",
                    ["nl"] = "Officieel AmusementParks.fun",
                    ["it"] = "Ufficiale AmusementParks.fun",
                    ["es"] = "Oficial AmusementParks.fun",
                    ["pl"] = "Oficjalne AmusementParks.fun",
                    ["pt"] = "Oficial AmusementParks.fun",
                },
                now),
            CreateSystemVideoTag(
                "video-tag-associated-creator",
                "associated-creator",
                new Dictionary<string, string>
                {
                    ["fr"] = "Createur contenu associe",
                    ["en"] = "Associated content creator",
                    ["de"] = "Verbundener Content Creator",
                    ["nl"] = "Geassocieerde contentmaker",
                    ["it"] = "Creatore di contenuti associato",
                    ["es"] = "Creador de contenido asociado",
                    ["pl"] = "Powiazany tworca tresci",
                    ["pt"] = "Criador de conteudo associado",
                },
                now),
            CreateSystemVideoTag(
                "video-tag-third-party-creator",
                "third-party-creator",
                new Dictionary<string, string>
                {
                    ["fr"] = "Createur contenu tiers",
                    ["en"] = "Third-party content creator",
                    ["de"] = "Externer Content Creator",
                    ["nl"] = "Externe contentmaker",
                    ["it"] = "Creatore di contenuti terzo",
                    ["es"] = "Creador de contenido externo",
                    ["pl"] = "Zewnetrzny tworca tresci",
                    ["pt"] = "Criador de conteudo terceiro",
                },
                now),
            CreateSystemVideoTag(
                "video-tag-official-park",
                "official-park",
                new Dictionary<string, string>
                {
                    ["fr"] = "Officiel parc ou exploitant",
                    ["en"] = "Official park or operator",
                    ["de"] = "Offizieller Park oder Betreiber",
                    ["nl"] = "Officieel park of exploitant",
                    ["it"] = "Parco o gestore ufficiale",
                    ["es"] = "Parque u operador oficial",
                    ["pl"] = "Oficjalny park lub operator",
                    ["pt"] = "Parque ou operador oficial",
                },
                now),
        };

        foreach (VideoTagDocument tag in tags)
        {
            FilterDefinition<VideoTagDocument> filter = Builders<VideoTagDocument>.Filter.Eq(static document => document.Slug, tag.Slug);
            UpdateDefinition<VideoTagDocument> update = Builders<VideoTagDocument>.Update
                .SetOnInsert(static document => document.Id, tag.Id)
                .Set(static document => document.Slug, tag.Slug)
                .Set(static document => document.Labels, tag.Labels)
                .Set(static document => document.Descriptions, tag.Descriptions)
                .Set(static document => document.IsActive, true)
                .SetOnInsert(static document => document.CreatedAt, tag.CreatedAt)
                .Set(static document => document.UpdatedAt, now);

            await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
    }

    private static VideoTagDocument CreateSystemVideoTag(string id, string slug, IReadOnlyDictionary<string, string> labels, DateTime now)
    {
        return new VideoTagDocument
        {
            Id = id,
            Slug = slug,
            Labels = labels.Select(static label => new LocalizedTextDocument
            {
                LanguageCode = label.Key,
                Value = label.Value,
            }).ToList(),
            Descriptions = new List<LocalizedTextDocument>(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
