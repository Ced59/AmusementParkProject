using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class ParkFitOperationalStatusMongoMapperTests
{
    [Fact]
    public void ToDocumentToDomain_ShouldPreserveTheVersionedDecisionHistory()
    {
        DateTime suspendedAtUtc = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);
        DateTime restoredAtUtc = suspendedAtUtc.AddHours(1);
        ParkFitOperationalStatus status = ParkFitOperationalStatus.CreateActive("park-1");
        status.Suspend("admin-1", "Calendrier à vérifier", suspendedAtUtc);
        status.RestoreRecommendations("admin-2", "Calendrier corrigé", restoredAtUtc);

        ParkFitOperationalStatus restored = status.ToDocument().ToDomain();

        Assert.Equal(ParkFitRecommendationState.Active, restored.State);
        Assert.Equal(2, restored.Revision);
        Assert.Equal(restoredAtUtc, restored.UpdatedAtUtc);
        ParkFitOperationalDecision[] decisions = restored.Decisions.ToArray();
        Assert.Equal(2, decisions.Length);
        Assert.Equal(ParkFitOperationalDecisionType.Suspended, decisions[0].Type);
        Assert.Equal("admin-1", decisions[0].ActorUserId);
        Assert.Equal(ParkFitOperationalDecisionType.Restored, decisions[1].Type);
        Assert.Equal("admin-2", decisions[1].ActorUserId);
    }
}
