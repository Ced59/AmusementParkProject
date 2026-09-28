using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class LiveTargetMappingMongoMapperTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveTheAuditableMappingRevision()
    {
        DateTime discoveredAtUtc = new DateTime(2026, 9, 28, 17, 0, 0, DateTimeKind.Utc);
        ExternalLiveTargetMapping mapping = ExternalLiveTargetMapping.CreateCandidate(
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                LiveDataSourceId.Parse("themeparks-wiki"),
                new ExternalLiveTargetDescriptor(
                    LiveTargetType.ParkItem,
                    "external-item-1",
                    "external-park-1",
                    "Black Mamba",
                    "Phantasialand",
                    "DE"),
                null,
                LiveMappingConfidence.Low,
                discoveredAtUtc)
            .Verify(
                new LiveTargetReference(
                    LiveTargetType.ParkItem,
                    "item-1",
                    "park-1",
                    "Black Mamba",
                    "Phantasialand",
                    "DE"),
                "admin-1",
                "Identifiants vérifiés.",
                discoveredAtUtc.AddMinutes(5));

        ExternalLiveTargetMappingDocument document = mapping.ToDocument();
        ExternalLiveTargetMapping restored = document.ToDomain();

        Assert.Equal("10000000000000000000000000000001:2", document.Id);
        Assert.Equal(mapping.RecordedAtUtc, document.CreatedAt);
        Assert.Equal(mapping.RecordedAtUtc, document.UpdatedAt);
        Assert.Equal(mapping.Version, restored.Version);
        Assert.Equal(mapping.SourceId, restored.SourceId);
        Assert.Equal(mapping.ExternalTarget.Id, restored.ExternalTarget.Id);
        Assert.Equal(mapping.Target?.Id, restored.Target?.Id);
        Assert.Equal(mapping.ReviewedByUserId, restored.ReviewedByUserId);
        Assert.Equal(mapping.ReviewNote, restored.ReviewNote);
        Assert.True(restored.IsEligibleForLiveUse);
    }
}
