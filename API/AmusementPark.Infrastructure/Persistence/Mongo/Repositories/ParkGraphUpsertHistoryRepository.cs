using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkGraphUpserts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkGraphUpsertHistoryRepository : IParkGraphUpsertHistoryRepository
{
    private readonly IMongoCollection<ParkGraphUpsertHistoryDocument> collection;
    private readonly int retentionDays;

    public ParkGraphUpsertHistoryRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkGraphUpsertHistoryDocument>(settings.ParkGraphUpsertHistoryCollectionName);
        this.retentionDays = Math.Max(1, settings.ParkGraphUpsertHistoryRetentionDays);
    }

    public async Task SaveAsync(ParkGraphUpsertHistoryEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string serializedResult = JsonSerializer.Serialize(entry.Result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        ParkGraphUpsertHistoryDocument document = new ParkGraphUpsertHistoryDocument
        {
            Id = entry.Id,
            OperationKind = entry.OperationKind,
            TargetParkId = entry.TargetParkId,
            TargetParkName = entry.TargetParkName,
            RequestedByUserId = entry.RequestedByUserId,
            ExpiresAt = entry.CreatedAtUtc.AddDays(this.retentionDays),
            RawJson = entry.RawJson,
            Result = BsonDocument.Parse(serializedResult),
            CreatedAt = entry.CreatedAtUtc,
            UpdatedAt = entry.CreatedAtUtc,
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParkGraphUpsertHistoryEntry>> ListRecentAsync(ParkGraphUpsertHistoryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        FilterDefinition<ParkGraphUpsertHistoryDocument> filter = string.IsNullOrWhiteSpace(query.TargetParkId)
            ? Builders<ParkGraphUpsertHistoryDocument>.Filter.Empty
            : Builders<ParkGraphUpsertHistoryDocument>.Filter.Eq(document => document.TargetParkId, query.TargetParkId);

        List<ParkGraphUpsertHistoryDocument> documents = await this.collection
            .Find(filter)
            .SortByDescending(document => document.CreatedAt)
            .Limit(query.Limit)
            .ToListAsync(cancellationToken);

        return documents.Select(MapToEntry).ToList();
    }

    private static ParkGraphUpsertHistoryEntry MapToEntry(ParkGraphUpsertHistoryDocument document)
    {
        return new ParkGraphUpsertHistoryEntry
        {
            Id = document.Id,
            OperationKind = document.OperationKind,
            TargetParkId = document.TargetParkId,
            TargetParkName = document.TargetParkName,
            RequestedByUserId = document.RequestedByUserId,
            CreatedAtUtc = document.CreatedAt,
            RawJson = document.RawJson,
            Result = MapResult(document.Result),
        };
    }

    private static ParkGraphUpsertResult MapResult(BsonDocument document)
    {
        ParkGraphUpsertResult result = new ParkGraphUpsertResult
        {
            OperationId = ReadString(document, "operationId") ?? string.Empty,
            Mode = ReadString(document, "mode") ?? "merge",
            IsApplied = ReadBool(document, "isApplied"),
            CanApply = ReadBool(document, "canApply"),
            PreviewedAtUtc = ReadDate(document, "previewedAtUtc") ?? DateTime.UtcNow,
            AppliedAtUtc = ReadDate(document, "appliedAtUtc"),
            TargetParkId = ReadString(document, "targetParkId"),
            TargetParkName = ReadString(document, "targetParkName"),
            Counts = MapCounts(ReadDocument(document, "counts")),
            Warnings = ReadStringArray(document, "warnings"),
            Errors = ReadStringArray(document, "errors"),
        };

        return result;
    }

    private static ParkGraphUpsertCounts MapCounts(BsonDocument? document)
    {
        if (document is null)
        {
            return new ParkGraphUpsertCounts();
        }

        return new ParkGraphUpsertCounts
        {
            Created = ReadInt(document, "created"),
            Updated = ReadInt(document, "updated"),
            Unchanged = ReadInt(document, "unchanged"),
            Warnings = ReadInt(document, "warnings"),
            Errors = ReadInt(document, "errors"),
        };
    }

    private static BsonDocument? ReadDocument(BsonDocument document, string propertyName)
    {
        if (!document.TryGetValue(propertyName, out BsonValue? value) || !value.IsBsonDocument)
        {
            return null;
        }

        return value.AsBsonDocument;
    }

    private static string? ReadString(BsonDocument document, string propertyName)
    {
        if (!document.TryGetValue(propertyName, out BsonValue? value) || value.IsBsonNull)
        {
            return null;
        }

        return value.IsString ? value.AsString : value.ToString();
    }

    private static bool ReadBool(BsonDocument document, string propertyName)
    {
        return document.TryGetValue(propertyName, out BsonValue? value) && value.IsBoolean && value.AsBoolean;
    }

    private static int ReadInt(BsonDocument document, string propertyName)
    {
        if (!document.TryGetValue(propertyName, out BsonValue? value))
        {
            return 0;
        }

        if (value.IsInt32)
        {
            return value.AsInt32;
        }

        if (value.IsInt64)
        {
            return (int)value.AsInt64;
        }

        return 0;
    }

    private static DateTime? ReadDate(BsonDocument document, string propertyName)
    {
        if (!document.TryGetValue(propertyName, out BsonValue? value) || value.IsBsonNull)
        {
            return null;
        }

        if (value.IsValidDateTime)
        {
            return value.ToUniversalTime();
        }

        if (value.IsString && DateTime.TryParse(value.AsString, out DateTime parsed))
        {
            return parsed.ToUniversalTime();
        }

        return null;
    }

    private static List<string> ReadStringArray(BsonDocument document, string propertyName)
    {
        if (!document.TryGetValue(propertyName, out BsonValue? value) || !value.IsBsonArray)
        {
            return new List<string>();
        }

        return value.AsBsonArray
            .Where(static item => !item.IsBsonNull)
            .Select(static item => item.IsString ? item.AsString : item.ToString())
            .Where(static item => item is not null)
            .Select(static item => item!)
            .ToList();
    }
}
