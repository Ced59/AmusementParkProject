using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.AdminAudit;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class AdminAuditLogWriterTests
{
    [Fact]
    public async Task WriteAsync_WhenDeterministicEntryAlreadyExists_ShouldRemainIdempotent()
    {
        Mock<IMongoCollection<AdminAuditLogDocument>> collection =
            new Mock<IMongoCollection<AdminAuditLogDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.InsertOneAsync(
                It.Is<AdminAuditLogDocument>(document =>
                    document.Id == "share-moderation-decision:job-1"),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .ThrowsAsync(CreateDuplicateKeyException());
        AdminAuditLogWriter writer = new AdminAuditLogWriter(collection.Object);
        AdminAuditLogEntry entry = new AdminAuditLogEntry
        {
            Id = "share-moderation-decision:job-1",
            OccurredAtUtc = new DateTime(2026, 9, 13, 21, 0, 0, DateTimeKind.Utc),
            Action = "share-moderation.report.decision-completed",
            EntityType = "ShareModerationReport",
            EntityId = "report-1",
            ActorUserId = "admin-1",
            HttpMethod = "BACKGROUND",
            Path = "share-moderation-decision",
            StatusCode = 200,
            TraceId = "job-1",
        };

        await writer.WriteAsync(entry, CancellationToken.None);

        collection.VerifyAll();
    }

    private static MongoWriteException CreateDuplicateKeyException()
    {
        ClusterId clusterId = new ClusterId();
        ServerId serverId = new ServerId(
            clusterId,
            new System.Net.DnsEndPoint("localhost", 27017));
        ConnectionId connectionId = new ConnectionId(serverId);
        WriteError error = (WriteError)Activator.CreateInstance(
            typeof(WriteError),
            System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic,
            null,
            new object[]
            {
                ServerErrorCategory.DuplicateKey,
                11000,
                "duplicate key",
                new BsonDocument(),
            },
            null)!;
        return new MongoWriteException(connectionId, error, null, null);
    }
}
