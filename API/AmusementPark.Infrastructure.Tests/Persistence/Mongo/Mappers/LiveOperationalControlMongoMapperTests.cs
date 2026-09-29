using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class LiveOperationalControlMongoMapperTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveVersionedTargetControl()
    {
        DateTime recordedAtUtc = new DateTime(2026, 9, 29, 9, 0, 0, DateTimeKind.Utc);
        LiveOperationalControl control = new LiveOperationalControl(
            Guid.NewGuid(),
            new LiveOperationalControlScope(
                LiveOperationalScopeType.Target,
                LiveDataSourceId.Parse("source"),
                "external-park",
                "park-1",
                LiveTargetType.ParkItem,
                "item-1"),
            false,
            false,
            2,
            1,
            "admin-1",
            "Provider incident",
            recordedAtUtc);

        LiveOperationalControlDocument document = control.ToDocument();
        LiveOperationalControl restored = document.ToDomain();

        Assert.Equal(control.Id, restored.Id);
        Assert.Equal(control.Scope, restored.Scope);
        Assert.Equal(2, restored.Revision);
        Assert.Equal(1, restored.SupersedesRevision);
        Assert.Equal(recordedAtUtc, restored.RecordedAtUtc);
        Assert.False(restored.CollectionEnabled);
        Assert.False(restored.PublicReadEnabled);
    }
}
