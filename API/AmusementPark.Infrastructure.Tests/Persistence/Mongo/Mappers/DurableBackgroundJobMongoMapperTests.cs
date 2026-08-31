using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class DurableBackgroundJobMongoMapperTests
{
    [Fact]
    public void ToApplication_ShouldCloneThePayloadAndPreserveOperationalMetadata()
    {
        using JsonDocument payload = JsonDocument.Parse("""{"scope":"world","revision":12}""");
        DateTime nowUtc = new DateTime(2026, 8, 31, 18, 0, 0, DateTimeKind.Utc);
        DurableBackgroundJobDocument document = new DurableBackgroundJobDocument
        {
            Id = "job-1",
            Kind = "rank.snapshot",
            NaturalKey = "world",
            PayloadVersion = 2,
            PayloadJson = payload.RootElement.ToStoredPayload(),
            RequestedRevision = 12,
            Status = DurableBackgroundJobStatus.Leased,
            Priority = 50,
            AttemptCount = 2,
            NotBeforeUtc = nowUtc,
            LeaseOwner = "worker-1",
            LeaseToken = "token-1",
            LeaseExpiresAtUtc = nowUtc.AddMinutes(2),
            CreatedAt = nowUtc.AddMinutes(-4),
            UpdatedAt = nowUtc,
            CorrelationId = "correlation-1",
        };

        DurableBackgroundJob mapped = document.ToApplication();

        Assert.Equal("job-1", mapped.Id);
        Assert.Equal("world", mapped.Payload.GetProperty("scope").GetString());
        Assert.Equal(12, mapped.Payload.GetProperty("revision").GetInt32());
        Assert.Equal(12, mapped.RequestedRevision);
        Assert.Equal(DurableBackgroundJobStatus.Leased, mapped.Status);
        Assert.Equal("token-1", mapped.LeaseToken);
        Assert.Equal("correlation-1", mapped.CorrelationId);
    }

    [Fact]
    public void StoredPayload_ShouldRoundTripOrdinaryJsonWithoutExtendedJsonInterpretation()
    {
        const string PayloadJson =
            "{\"$numberLong\":\"42\",\"fakeOid\":{\"$oid\":\"not-an-object-id\"},\"precise\":12345678901234567890.123456789}";
        using JsonDocument payload = JsonDocument.Parse(PayloadJson);
        string storedPayload = payload.RootElement.ToStoredPayload();
        DurableBackgroundJobDocument document = new DurableBackgroundJobDocument
        {
            PayloadJson = storedPayload,
        };

        DurableBackgroundJob mapped = document.ToApplication();

        Assert.Equal(PayloadJson, storedPayload);
        Assert.Equal("42", mapped.Payload.GetProperty("$numberLong").GetString());
        Assert.Equal("not-an-object-id", mapped.Payload.GetProperty("fakeOid").GetProperty("$oid").GetString());
        Assert.Equal("12345678901234567890.123456789", mapped.Payload.GetProperty("precise").GetRawText());
    }
}
