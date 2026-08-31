using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class DurableBackgroundJobMongoMapper
{
    private static readonly JsonWriterSettings PayloadJsonWriterSettings = new JsonWriterSettings
    {
        OutputMode = JsonOutputMode.RelaxedExtendedJson,
    };

    public static BsonDocument ToBsonPayload(this JsonElement payload)
    {
        return BsonDocument.Parse(payload.GetRawText());
    }

    public static DurableBackgroundJob ToApplication(this DurableBackgroundJobDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using JsonDocument payloadDocument = JsonDocument.Parse(
            document.Payload.ToJson(PayloadJsonWriterSettings));
        return new DurableBackgroundJob(
            document.Id,
            document.Kind,
            document.NaturalKey,
            document.IdempotencyKey,
            document.PayloadVersion,
            payloadDocument.RootElement.Clone(),
            document.RequestedRevision,
            document.ProcessedRevision,
            document.Status,
            document.Priority,
            document.AttemptCount,
            document.NotBeforeUtc,
            document.LeaseOwner,
            document.LeaseToken,
            document.LeaseExpiresAtUtc,
            document.CreatedAt,
            document.UpdatedAt,
            document.CompletedAtUtc,
            document.LastErrorCode,
            document.CorrelationId);
    }

    public static DurableBackgroundJobDiagnosticItem ToDiagnosticItem(
        this DurableBackgroundJobDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new DurableBackgroundJobDiagnosticItem(
            document.Id,
            document.Kind,
            document.NaturalKey,
            document.Status,
            document.Priority,
            document.AttemptCount,
            document.RequestedRevision,
            document.ProcessedRevision,
            document.NotBeforeUtc,
            document.LeaseExpiresAtUtc,
            document.CreatedAt,
            document.UpdatedAt,
            document.CompletedAtUtc,
            document.LastErrorCode,
            document.CorrelationId);
    }
}
