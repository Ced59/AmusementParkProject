using System.Security.Cryptography;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class PassportExportRepository : IPassportExportRepository
{
    internal const int ChunkSizeBytes = 1024 * 1024;
    private readonly IMongoCollection<PassportExportDocument> exports;
    private readonly IMongoCollection<PassportExportChunkDocument> chunks;

    public PassportExportRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(
            database.GetCollection<PassportExportDocument>(settings.PassportExportsCollectionName),
            database.GetCollection<PassportExportChunkDocument>(settings.PassportExportChunksCollectionName))
    {
    }

    internal PassportExportRepository(
        IMongoCollection<PassportExportDocument> exports,
        IMongoCollection<PassportExportChunkDocument> chunks)
    {
        this.exports = exports ?? throw new ArgumentNullException(nameof(exports));
        this.chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
    }

    public Task CreateAsync(PassportExport passportExport, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(passportExport);
        PassportExportDocument document = ToDocument(passportExport);
        return this.exports.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<PassportExport?> GetOwnedAsync(
        string exportId,
        string userId,
        CancellationToken cancellationToken)
    {
        PassportExportDocument? document = await this.exports
            .Find(BuildOwnedFilter(exportId, userId))
            .FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToApplication(document);
    }

    public async Task<bool> TryMarkProcessingAsync(
        string exportId,
        string userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<PassportExportDocument> filters = Builders<PassportExportDocument>.Filter;
        FilterDefinition<PassportExportDocument> filter = BuildOwnedFilter(exportId, userId)
            & filters.In(
                static document => document.Status,
                new[] { PassportExportStatus.Pending, PassportExportStatus.Processing })
            & filters.Gt(static document => document.ExpiresAtUtc, nowUtc);
        UpdateDefinition<PassportExportDocument> update = Builders<PassportExportDocument>.Update
            .Set(static document => document.Status, PassportExportStatus.Processing)
            .Set(static document => document.UpdatedAt, nowUtc)
            .Unset(static document => document.ErrorCode);
        UpdateResult result = await this.exports.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> TryCompleteAsync(
        string exportId,
        string userId,
        PassportExportArtifact artifact,
        DateTime completedAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        string generationId = Guid.NewGuid().ToString("N");
        List<PassportExportChunkDocument> chunkDocuments = CreateChunks(
            exportId,
            generationId,
            artifact.Content,
            completedAtUtc,
            expiresAtUtc);
        FilterDefinitionBuilder<PassportExportChunkDocument> chunkFilters =
            Builders<PassportExportChunkDocument>.Filter;
        FilterDefinition<PassportExportChunkDocument> generationFilter =
            chunkFilters.Eq(static document => document.ExportId, exportId)
            & chunkFilters.Eq(static document => document.GenerationId, generationId);
        if (chunkDocuments.Count > 0)
        {
            await this.chunks.InsertManyAsync(
                chunkDocuments,
                cancellationToken: cancellationToken);
        }

        FilterDefinitionBuilder<PassportExportDocument> filters = Builders<PassportExportDocument>.Filter;
        FilterDefinition<PassportExportDocument> exportFilter = BuildOwnedFilter(exportId, userId)
            & filters.In(
                static document => document.Status,
                new[] { PassportExportStatus.Pending, PassportExportStatus.Processing });
        UpdateDefinition<PassportExportDocument> update = Builders<PassportExportDocument>.Update
            .Set(static document => document.Status, PassportExportStatus.Ready)
            .Set(static document => document.SchemaVersion, artifact.SchemaVersion)
            .Set(static document => document.CompletedAtUtc, completedAtUtc)
            .Set(static document => document.UpdatedAt, completedAtUtc)
            .Set(static document => document.ExpiresAtUtc, expiresAtUtc)
            .Set(static document => document.FileName, artifact.FileName)
            .Set(static document => document.ContentType, artifact.ContentType)
            .Set(static document => document.SizeBytes, artifact.Content.LongLength)
            .Set(static document => document.ChunkCount, chunkDocuments.Count)
            .Set(static document => document.GenerationId, generationId)
            .Set(static document => document.ChecksumSha256, artifact.ChecksumSha256)
            .Unset(static document => document.ErrorCode);
        UpdateResult result = await this.exports.UpdateOneAsync(
            exportFilter,
            update,
            cancellationToken: cancellationToken);
        if (result.MatchedCount == 1)
        {
            await this.chunks.DeleteManyAsync(
                chunkFilters.Eq(static document => document.ExportId, exportId)
                & chunkFilters.Ne(static document => document.GenerationId, generationId),
                cancellationToken);
            return true;
        }

        await this.chunks.DeleteManyAsync(generationFilter, cancellationToken);
        return false;
    }

    public async Task<bool> TryFailAsync(
        string exportId,
        string userId,
        string errorCode,
        DateTime failedAtUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<PassportExportDocument> filters = Builders<PassportExportDocument>.Filter;
        FilterDefinition<PassportExportDocument> filter = BuildOwnedFilter(exportId, userId)
            & filters.In(
                static document => document.Status,
                new[] { PassportExportStatus.Pending, PassportExportStatus.Processing });
        UpdateDefinition<PassportExportDocument> update = Builders<PassportExportDocument>.Update
            .Set(static document => document.Status, PassportExportStatus.Failed)
            .Set(static document => document.ErrorCode, errorCode)
            .Set(static document => document.UpdatedAt, failedAtUtc)
            .Unset(static document => document.FileName)
            .Unset(static document => document.ContentType)
            .Unset(static document => document.SizeBytes)
            .Unset(static document => document.ChunkCount)
            .Unset(static document => document.GenerationId)
            .Unset(static document => document.ChecksumSha256);
        UpdateResult result = await this.exports.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        if (result.MatchedCount == 1)
        {
            await this.chunks.DeleteManyAsync(
                Builders<PassportExportChunkDocument>.Filter.Eq(
                    static document => document.ExportId,
                    exportId),
                cancellationToken);
        }

        return result.MatchedCount == 1;
    }

    public async Task<PassportExportDownload?> GetOwnedDownloadAsync(
        string exportId,
        string userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<PassportExportDocument> filters = Builders<PassportExportDocument>.Filter;
        PassportExportDocument? passportExport = await this.exports
            .Find(BuildOwnedFilter(exportId, userId)
                & filters.Eq(static document => document.Status, PassportExportStatus.Ready)
                & filters.Gt(static document => document.ExpiresAtUtc, nowUtc))
            .FirstOrDefaultAsync(cancellationToken);
        if (passportExport is null
            || passportExport.SizeBytes is not long sizeBytes
            || passportExport.ChunkCount is not int chunkCount
            || passportExport.GenerationId is null
            || passportExport.FileName is null
            || passportExport.ContentType is null
            || passportExport.ChecksumSha256 is null
            || sizeBytes < 0
            || sizeBytes > int.MaxValue)
        {
            return null;
        }

        List<PassportExportChunkDocument> chunkDocuments = await this.chunks
            .Find(Builders<PassportExportChunkDocument>.Filter.Eq(
                    static document => document.ExportId,
                    exportId)
                & Builders<PassportExportChunkDocument>.Filter.Eq(
                    static document => document.GenerationId,
                    passportExport.GenerationId))
            .SortBy(static document => document.Index)
            .ToListAsync(cancellationToken);
        if (chunkDocuments.Count != chunkCount)
        {
            return null;
        }

        using MemoryStream content = new MemoryStream(checked((int)sizeBytes));
        for (int index = 0; index < chunkDocuments.Count; index++)
        {
            PassportExportChunkDocument chunk = chunkDocuments[index];
            if (chunk.Index != index)
            {
                return null;
            }

            content.Write(chunk.Data);
        }

        byte[] bytes = content.ToArray();
        string checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (bytes.LongLength != sizeBytes
            || !string.Equals(checksum, passportExport.ChecksumSha256, StringComparison.Ordinal))
        {
            return null;
        }

        return new PassportExportDownload(
            passportExport.FileName,
            passportExport.ContentType,
            bytes,
            checksum);
    }

    public async Task<IReadOnlyCollection<PassportExport>> ListPendingForReconciliationAsync(
        DateTime maximumUpdatedAtUtc,
        DateTime minimumExpiresAtUtc,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        if (maximumCount is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        FilterDefinitionBuilder<PassportExportDocument> filters = Builders<PassportExportDocument>.Filter;
        List<PassportExportDocument> documents = await this.exports
            .Find(filters.Eq(static document => document.Status, PassportExportStatus.Pending)
                & filters.Lte(static document => document.UpdatedAt, maximumUpdatedAtUtc)
                & filters.Gt(static document => document.ExpiresAtUtc, minimumExpiresAtUtc))
            .SortBy(static document => document.UpdatedAt)
            .Limit(maximumCount)
            .ToListAsync(cancellationToken);
        return documents.Select(ToApplication).ToArray();
    }

    public async Task<int> FailStaleProcessingAsync(
        DateTime maximumUpdatedAtUtc,
        DateTime minimumExpiresAtUtc,
        string errorCode,
        DateTime failedAtUtc,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        if (maximumCount is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        string normalizedErrorCode = string.IsNullOrWhiteSpace(errorCode)
            ? throw new ArgumentException("An error code is required.", nameof(errorCode))
            : errorCode.Trim();
        FilterDefinitionBuilder<PassportExportDocument> filters =
            Builders<PassportExportDocument>.Filter;
        FilterDefinition<PassportExportDocument> staleFilter =
            filters.Eq(
                static document => document.Status,
                PassportExportStatus.Processing)
            & filters.Lte(
                static document => document.UpdatedAt,
                maximumUpdatedAtUtc)
            & filters.Gt(
                static document => document.ExpiresAtUtc,
                minimumExpiresAtUtc);
        List<string> candidateIds = await this.exports
            .Find(staleFilter)
            .SortBy(static document => document.UpdatedAt)
            .Project(static document => document.Id)
            .Limit(maximumCount)
            .ToListAsync(cancellationToken);
        if (candidateIds.Count == 0)
        {
            return 0;
        }

        FilterDefinition<PassportExportDocument> updateFilter = staleFilter
            & filters.In(static document => document.Id, candidateIds);
        UpdateDefinition<PassportExportDocument> update =
            Builders<PassportExportDocument>.Update
                .Set(static document => document.Status, PassportExportStatus.Failed)
                .Set(static document => document.ErrorCode, normalizedErrorCode)
                .Set(static document => document.UpdatedAt, failedAtUtc)
                .Unset(static document => document.FileName)
                .Unset(static document => document.ContentType)
                .Unset(static document => document.SizeBytes)
                .Unset(static document => document.ChunkCount)
                .Unset(static document => document.GenerationId)
                .Unset(static document => document.ChecksumSha256);
        UpdateResult result = await this.exports.UpdateManyAsync(
            updateFilter,
            update,
            cancellationToken: cancellationToken);
        return checked((int)result.ModifiedCount);
    }

    public async Task InvalidateOwnedAsync(
        string userId,
        DateTime createdAtOrBeforeUtc,
        DateTime invalidatedAtUtc,
        CancellationToken cancellationToken)
    {
        if (createdAtOrBeforeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The export creation fence must be UTC.",
                nameof(createdAtOrBeforeUtc));
        }

        if (invalidatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The invalidation timestamp must be UTC.", nameof(invalidatedAtUtc));
        }

        string normalizedUserId = string.IsNullOrWhiteSpace(userId)
            ? throw new ArgumentException("A user identifier is required.", nameof(userId))
            : userId.Trim();
        FilterDefinition<PassportExportDocument> accessibleFilter =
            BuildInvalidationFilter(
                normalizedUserId,
                createdAtOrBeforeUtc,
                invalidatedAtUtc);
        List<string> exportIds = await this.exports.Find(accessibleFilter)
            .Project(static document => document.Id)
            .ToListAsync(cancellationToken);
        if (exportIds.Count == 0)
        {
            return;
        }

        UpdateDefinition<PassportExportDocument> update =
            Builders<PassportExportDocument>.Update
                .Set(static document => document.Status, PassportExportStatus.Failed)
                .Set(static document => document.ErrorCode, "passport-export.data-changed")
                .Set(static document => document.ExpiresAtUtc, invalidatedAtUtc)
                .Set(static document => document.UpdatedAt, invalidatedAtUtc)
                .Unset(static document => document.FileName)
                .Unset(static document => document.ContentType)
                .Unset(static document => document.SizeBytes)
                .Unset(static document => document.ChunkCount)
                .Unset(static document => document.GenerationId)
                .Unset(static document => document.ChecksumSha256);
        await this.exports.UpdateManyAsync(
            accessibleFilter,
            update,
            cancellationToken: cancellationToken);
        await this.chunks.DeleteManyAsync(
            Builders<PassportExportChunkDocument>.Filter.In(
                static document => document.ExportId,
                exportIds),
            cancellationToken);
    }

    internal static FilterDefinition<PassportExportDocument> BuildInvalidationFilter(
        string userId,
        DateTime createdAtOrBeforeUtc,
        DateTime invalidatedAtUtc)
    {
        FilterDefinitionBuilder<PassportExportDocument> filters =
            Builders<PassportExportDocument>.Filter;
        return filters.Eq(static document => document.UserId, userId)
            & filters.Lte(static document => document.CreatedAt, createdAtOrBeforeUtc)
            & filters.Gt(static document => document.ExpiresAtUtc, invalidatedAtUtc);
    }

    private static FilterDefinition<PassportExportDocument> BuildOwnedFilter(
        string exportId,
        string userId)
    {
        FilterDefinitionBuilder<PassportExportDocument> filters = Builders<PassportExportDocument>.Filter;
        return filters.Eq(static document => document.Id, exportId.Trim())
            & filters.Eq(static document => document.UserId, userId.Trim());
    }

    private static List<PassportExportChunkDocument> CreateChunks(
        string exportId,
        string generationId,
        byte[] content,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        List<PassportExportChunkDocument> documents = new List<PassportExportChunkDocument>();
        for (int offset = 0; offset < content.Length; offset += ChunkSizeBytes)
        {
            int index = documents.Count;
            int length = Math.Min(ChunkSizeBytes, content.Length - offset);
            byte[] data = new byte[length];
            Buffer.BlockCopy(content, offset, data, 0, length);
            documents.Add(new PassportExportChunkDocument
            {
                Id = $"{exportId}:{generationId}:{index:D8}",
                ExportId = exportId,
                GenerationId = generationId,
                Index = index,
                Data = data,
                ExpiresAtUtc = expiresAtUtc,
                CreatedAt = createdAtUtc,
                UpdatedAt = createdAtUtc,
            });
        }

        return documents;
    }

    private static PassportExportDocument ToDocument(PassportExport passportExport)
    {
        return new PassportExportDocument
        {
            Id = passportExport.Id,
            UserId = passportExport.UserId,
            Format = passportExport.Format,
            Status = passportExport.Status,
            SchemaVersion = passportExport.SchemaVersion,
            ExpiresAtUtc = passportExport.ExpiresAtUtc,
            CompletedAtUtc = passportExport.CompletedAtUtc,
            FileName = passportExport.FileName,
            ContentType = passportExport.ContentType,
            SizeBytes = passportExport.SizeBytes,
            ChecksumSha256 = passportExport.ChecksumSha256,
            ErrorCode = passportExport.ErrorCode,
            CreatedAt = passportExport.CreatedAtUtc,
            UpdatedAt = passportExport.UpdatedAtUtc,
        };
    }

    private static PassportExport ToApplication(PassportExportDocument document)
    {
        return new PassportExport(
            document.Id,
            document.UserId,
            document.Format,
            document.Status,
            document.SchemaVersion,
            document.CreatedAt,
            document.UpdatedAt,
            document.ExpiresAtUtc,
            document.CompletedAtUtc,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.ChecksumSha256,
            document.ErrorCode);
    }
}
