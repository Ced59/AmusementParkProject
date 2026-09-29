using System.Globalization;
using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class LiveTargetMappingRepository : ILiveTargetMappingRepository
{
    private readonly IMongoCollection<ExternalLiveTargetMappingDocument> collection;

    public LiveTargetMappingRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ExternalLiveTargetMappingDocument>(
            settings.LiveTargetMappingsCollectionName);
    }

    public async Task<ExternalLiveTargetMapping?> GetLatestByIdAsync(
        Guid mappingId,
        CancellationToken cancellationToken)
    {
        if (mappingId == Guid.Empty)
        {
            return null;
        }

        string normalizedId = mappingId.ToString("N", CultureInfo.InvariantCulture);
        ExternalLiveTargetMappingDocument? document = await this.collection
            .Find(item => item.MappingId == normalizedId)
            .SortByDescending(static item => item.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<ExternalLiveTargetMapping?> GetLatestByNaturalKeyAsync(
        LiveDataSourceId sourceId,
        string externalTargetId,
        CancellationToken cancellationToken)
    {
        string normalizedExternalTargetId = externalTargetId?.Trim() ?? string.Empty;
        if (normalizedExternalTargetId.Length == 0)
        {
            return null;
        }

        string normalizedSourceId = sourceId.Value;
        ExternalLiveTargetMappingDocument? document = await this.collection
            .Find(item => item.SourceId == normalizedSourceId
                && item.ExternalTarget.Id == normalizedExternalTargetId)
            .SortByDescending(static item => item.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<ExternalLiveTargetMapping>> GetLatestByExternalTargetIdsAsync(
        LiveDataSourceId sourceId,
        IReadOnlyCollection<string> externalTargetIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(externalTargetIds);
        string[] normalizedIds = externalTargetIds
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return Array.Empty<ExternalLiveTargetMapping>();
        }

        List<BsonDocument> stages = new List<BsonDocument>
        {
            new BsonDocument("$match", new BsonDocument
            {
                ["sourceId"] = sourceId.Value,
                ["externalTarget.id"] = new BsonDocument(
                    "$in",
                    new BsonArray(normalizedIds)),
            }),
            new BsonDocument("$sort", new BsonDocument
            {
                ["externalTarget.id"] = 1,
                ["revision"] = -1,
            }),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$externalTarget.id",
                ["document"] = new BsonDocument("$first", "$$ROOT"),
            }),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$document")),
        };
        PipelineDefinition<ExternalLiveTargetMappingDocument, BsonDocument> pipeline =
            PipelineDefinition<ExternalLiveTargetMappingDocument, BsonDocument>.Create(stages);
        List<BsonDocument> documents = await this.collection.Aggregate(pipeline)
            .ToListAsync(cancellationToken);
        return documents
            .Select(static document => BsonSerializer.Deserialize<ExternalLiveTargetMappingDocument>(document))
            .Select(static document => document.ToDomain())
            .ToArray();
    }

    public async Task<IReadOnlyCollection<string>> GetEligibleInternalTargetIdsByParkAsync(
        string internalParkId,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = internalParkId?.Trim() ?? string.Empty;
        if (normalizedParkId.Length == 0)
        {
            return Array.Empty<string>();
        }

        List<BsonDocument> stages = new List<BsonDocument>
        {
            new BsonDocument("$sort", new BsonDocument
            {
                ["mappingId"] = 1,
                ["revision"] = -1,
            }),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$mappingId",
                ["document"] = new BsonDocument("$first", "$$ROOT"),
            }),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$document")),
            new BsonDocument("$match", new BsonDocument
            {
                ["status"] = LiveMappingStatus.Verified.ToString(),
                ["validToUtc"] = BsonNull.Value,
                ["target.parkId"] = normalizedParkId,
                ["target.id"] = new BsonDocument("$type", "string"),
            }),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$target.id",
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1)),
        };
        PipelineDefinition<ExternalLiveTargetMappingDocument, BsonDocument> pipeline =
            PipelineDefinition<ExternalLiveTargetMappingDocument, BsonDocument>.Create(stages);
        List<BsonDocument> documents = await this.collection.Aggregate(pipeline)
            .ToListAsync(cancellationToken);
        return documents
            .Select(static document => document["_id"].AsString)
            .ToArray();
    }

    public async Task<PagedResult<ExternalLiveTargetMapping>> SearchLatestAsync(
        LiveTargetMappingSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        long offset = ((long)criteria.Page - 1L) * criteria.PageSize;
        List<BsonDocument> stages = new List<BsonDocument>();
        BsonDocument immutableMatch = BuildImmutableMatch(criteria);
        if (immutableMatch.ElementCount > 0)
        {
            stages.Add(new BsonDocument("$match", immutableMatch));
        }

        stages.Add(new BsonDocument("$sort", new BsonDocument
        {
            ["mappingId"] = 1,
            ["revision"] = -1,
        }));
        stages.Add(new BsonDocument("$group", new BsonDocument
        {
            ["_id"] = "$mappingId",
            ["document"] = new BsonDocument("$first", "$$ROOT"),
        }));
        stages.Add(new BsonDocument("$replaceRoot", new BsonDocument(
            "newRoot",
            "$document")));
        BsonDocument currentMatch = BuildCurrentMatch(criteria);
        if (currentMatch.ElementCount > 0)
        {
            stages.Add(new BsonDocument("$match", currentMatch));
        }

        stages.Add(new BsonDocument("$sort", new BsonDocument
        {
            ["recordedAtUtc"] = -1,
            ["mappingId"] = 1,
        }));
        stages.Add(new BsonDocument("$facet", new BsonDocument
        {
            ["metadata"] = new BsonArray
            {
                new BsonDocument("$count", "total"),
            },
            ["items"] = new BsonArray
            {
                new BsonDocument("$skip", offset),
                new BsonDocument("$limit", criteria.PageSize),
            },
        }));
        PipelineDefinition<ExternalLiveTargetMappingDocument, BsonDocument> pipeline =
            PipelineDefinition<ExternalLiveTargetMappingDocument, BsonDocument>.Create(stages);
        BsonDocument? result = await this.collection.Aggregate(pipeline)
            .FirstOrDefaultAsync(cancellationToken);
        if (result is null)
        {
            return new PagedResult<ExternalLiveTargetMapping>(
                Array.Empty<ExternalLiveTargetMapping>(),
                criteria.Page,
                criteria.PageSize,
                0);
        }

        BsonArray metadata = result["metadata"].AsBsonArray;
        long total = metadata.Count == 0
            ? 0
            : metadata[0].AsBsonDocument["total"].ToInt64();
        ExternalLiveTargetMapping[] mappings = result["items"].AsBsonArray
            .Select(static value => BsonSerializer.Deserialize<ExternalLiveTargetMappingDocument>(
                value.AsBsonDocument))
            .Select(static document => document.ToDomain())
            .ToArray();
        return new PagedResult<ExternalLiveTargetMapping>(
            mappings,
            criteria.Page,
            criteria.PageSize,
            total);
    }

    public async Task<LiveTargetMappingWriteOutcome> AppendRevisionAsync(
        ExternalLiveTargetMapping mapping,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        if (expectedRevision < 0 || mapping.Revision != expectedRevision + 1)
        {
            return LiveTargetMappingWriteOutcome.Conflict;
        }

        ExternalLiveTargetMapping? existingByNaturalKey = await this.GetLatestByNaturalKeyAsync(
            mapping.SourceId,
            mapping.ExternalTarget.Id,
            cancellationToken);
        if (expectedRevision == 0)
        {
            if (existingByNaturalKey is not null)
            {
                return LiveTargetMappingWriteOutcome.AlreadyExists;
            }
        }
        else
        {
            ExternalLiveTargetMapping? current = await this.GetLatestByIdAsync(
                mapping.Id,
                cancellationToken);
            if (current is null
                || current.Revision != expectedRevision
                || existingByNaturalKey is null
                || existingByNaturalKey.Id != mapping.Id)
            {
                return LiveTargetMappingWriteOutcome.Conflict;
            }
        }

        try
        {
            await this.collection.InsertOneAsync(
                mapping.ToDocument(),
                cancellationToken: cancellationToken);
            return LiveTargetMappingWriteOutcome.Created;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return expectedRevision == 0
                ? LiveTargetMappingWriteOutcome.AlreadyExists
                : LiveTargetMappingWriteOutcome.Conflict;
        }
    }

    private static BsonDocument BuildImmutableMatch(LiveTargetMappingSearchCriteria criteria)
    {
        BsonDocument match = new BsonDocument();
        string? sourceId = string.IsNullOrWhiteSpace(criteria.SourceId)
            ? null
            : criteria.SourceId.Trim();
        if (sourceId is not null)
        {
            match["sourceId"] = sourceId;
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            string pattern = Regex.Escape(criteria.Search.Trim());
            BsonRegularExpression expression = new BsonRegularExpression(pattern, "i");
            match["$or"] = new BsonArray
            {
                new BsonDocument("externalTarget.id", expression),
                new BsonDocument("externalTarget.displayName", expression),
                new BsonDocument("externalTarget.parentDisplayName", expression),
            };
        }

        return match;
    }

    private static BsonDocument BuildCurrentMatch(LiveTargetMappingSearchCriteria criteria)
    {
        BsonDocument match = new BsonDocument();
        if (criteria.Status.HasValue)
        {
            match["status"] = criteria.Status.Value.ToString();
        }

        if (criteria.TargetType.HasValue)
        {
            match["externalTarget.type"] = criteria.TargetType.Value.ToString();
        }

        return match;
    }
}
