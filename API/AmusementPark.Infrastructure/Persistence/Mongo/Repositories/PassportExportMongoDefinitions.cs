using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class PassportExportMongoDefinitions
{
    public static IReadOnlyCollection<CreateIndexModel<PassportExportDocument>> BuildExportIndexes()
    {
        return new[]
        {
            new CreateIndexModel<PassportExportDocument>(
                Builders<PassportExportDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_passport_exports_owner_created" }),
            new CreateIndexModel<PassportExportDocument>(
                Builders<PassportExportDocument>.IndexKeys
                    .Ascending(static document => document.Status)
                    .Ascending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = "idx_passport_exports_reconciliation" }),
            new CreateIndexModel<PassportExportDocument>(
                Builders<PassportExportDocument>.IndexKeys
                    .Ascending(static document => document.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "idx_passport_exports_expiry_ttl",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };
    }

    public static IReadOnlyCollection<CreateIndexModel<PassportExportChunkDocument>> BuildChunkIndexes()
    {
        return new[]
        {
            new CreateIndexModel<PassportExportChunkDocument>(
                Builders<PassportExportChunkDocument>.IndexKeys
                    .Ascending(static document => document.ExportId)
                    .Ascending(static document => document.GenerationId)
                    .Ascending(static document => document.Index),
                new CreateIndexOptions { Name = "idx_passport_export_chunks_order", Unique = true }),
            new CreateIndexModel<PassportExportChunkDocument>(
                Builders<PassportExportChunkDocument>.IndexKeys
                    .Ascending(static document => document.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "idx_passport_export_chunks_expiry_ttl",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };
    }
}
