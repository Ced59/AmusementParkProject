using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitOperationalStatusTests
{
    [Fact]
    public void ActivateThenDeactivate_ShouldKeepPortfolioDecisionsAuditable()
    {
        ParkFitOperationalStatus status = ParkFitOperationalStatus.CreateNotActivated("park-1");
        DateTime activatedAtUtc = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc);
        DateTime deactivatedAtUtc = activatedAtUtc.AddHours(2);

        status.Activate("admin-1", "Données validées", activatedAtUtc);
        status.Deactivate("admin-2", "Retrait du portefeuille", deactivatedAtUtc);

        Assert.Equal(ParkFitRecommendationState.NotActivated, status.State);
        Assert.Equal(2, status.Revision);
        Assert.Equal(deactivatedAtUtc, status.UpdatedAtUtc);
        Assert.Collection(
            status.Decisions,
            decision => Assert.Equal(ParkFitOperationalDecisionType.Activated, decision.Type),
            decision => Assert.Equal(ParkFitOperationalDecisionType.Deactivated, decision.Type));
    }

    [Fact]
    public void SuspendThenRestore_ShouldVersionAndKeepAuditableDecisions()
    {
        ParkFitOperationalStatus status = ParkFitOperationalStatus.CreateActive("park-1");
        DateTime suspendedAtUtc = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);
        DateTime restoredAtUtc = suspendedAtUtc.AddHours(1);

        status.Suspend("admin-1", "Source officielle à revérifier", suspendedAtUtc);
        status.RestoreRecommendations("admin-2", "Source officielle corrigée", restoredAtUtc);

        Assert.Equal(ParkFitRecommendationState.Active, status.State);
        Assert.Equal(2, status.Revision);
        Assert.Equal(restoredAtUtc, status.UpdatedAtUtc);
        Assert.Collection(
            status.Decisions,
            decision => Assert.Equal(ParkFitOperationalDecisionType.Suspended, decision.Type),
            decision => Assert.Equal(ParkFitOperationalDecisionType.Restored, decision.Type));
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldRejectTransition()
    {
        ParkFitOperationalStatus status = ParkFitOperationalStatus.CreateActive("park-1");
        DateTime timestampUtc = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);
        status.Suspend("admin-1", "Vérification en cours", timestampUtc);

        Assert.Throws<InvalidOperationException>(() =>
            status.Suspend("admin-1", "Deuxième suspension", timestampUtc));
    }

    [Fact]
    public void Suspend_WithMarkupInReason_ShouldReject()
    {
        ParkFitOperationalStatus status = ParkFitOperationalStatus.CreateActive("park-1");
        DateTime timestampUtc = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() =>
            status.Suspend("admin-1", "<strong>Suspension</strong>", timestampUtc));
    }

    [Fact]
    public void Restore_WithTruncatedAlternatingHistory_ShouldRemainValid()
    {
        DateTime timestampUtc = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);
        ParkFitOperationalDecision decision = new ParkFitOperationalDecision(
            ParkFitOperationalDecisionType.Restored,
            "admin-1",
            "Donnée corrigée",
            timestampUtc,
            52);

        ParkFitOperationalStatus status = ParkFitOperationalStatus.Restore(
            "park-1",
            ParkFitRecommendationState.Active,
            52,
            timestampUtc,
            new[] { decision });

        Assert.Equal(ParkFitRecommendationState.Active, status.State);
    }

    [Fact]
    public void Restore_WithRevisionButNoHistory_ShouldRejectCorruptState()
    {
        Assert.Throws<ArgumentException>(() => ParkFitOperationalStatus.Restore(
            "park-1",
            ParkFitRecommendationState.Suspended,
            1,
            new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc),
            Array.Empty<ParkFitOperationalDecision>()));
    }

    [Fact]
    public void Deactivate_WhenSuspended_ShouldNeverRestoreRecommendationsInBetween()
    {
        ParkFitOperationalStatus status = ParkFitOperationalStatus.CreateActive("park-1");
        DateTime timestampUtc = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);
        status.Suspend("admin-1", "Contrôle requis", timestampUtc);

        status.Deactivate("admin-1", "Retrait du portefeuille", timestampUtc.AddMinutes(1));

        Assert.Equal(ParkFitRecommendationState.NotActivated, status.State);
        Assert.Equal(
            ParkFitOperationalDecisionType.DeactivatedDuringSuspension,
            status.Decisions.Last().Type);
    }

    [Fact]
    public void Restore_WithMixedTruncatedHistory_ShouldInferTheRequiredInitialState()
    {
        DateTime activatedAtUtc = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);
        DateTime suspendedAtUtc = activatedAtUtc.AddHours(1);
        ParkFitOperationalStatus status = ParkFitOperationalStatus.Restore(
            "park-1",
            ParkFitRecommendationState.Suspended,
            52,
            suspendedAtUtc,
            new[]
            {
                new ParkFitOperationalDecision(
                    ParkFitOperationalDecisionType.Activated,
                    "admin-1",
                    "Données validées",
                    activatedAtUtc,
                    51),
                new ParkFitOperationalDecision(
                    ParkFitOperationalDecisionType.Suspended,
                    "admin-2",
                    "Contrôle requis",
                    suspendedAtUtc,
                    52),
            });

        Assert.Equal(ParkFitRecommendationState.Suspended, status.State);
    }
}
