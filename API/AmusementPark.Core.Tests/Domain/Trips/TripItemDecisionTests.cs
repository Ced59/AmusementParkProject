using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripItemDecisionTests
{
    private static readonly DateTime CreatedAtUtc = new(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Set_ShouldAuditTheLatestAuthorReasonAndVersion()
    {
        TripItemDecision decision = CreateDecision();

        decision.Set(
            TripItemDecisionStatus.SplitGroup,
            "Une partie du groupe fait le tour.",
            "editor-2",
            CreatedAtUtc.AddMinutes(1));

        Assert.Equal(TripItemDecisionStatus.SplitGroup, decision.Status);
        Assert.Equal("Une partie du groupe fait le tour.", decision.Reason);
        Assert.Equal("editor-2", decision.DecidedByUserId);
        Assert.Equal(2, decision.Version);
    }

    [Fact]
    public void Create_WithoutMeaningfulReason_ShouldRejectTheDecision()
    {
        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() =>
            TripItemDecision.Create(
                TripItemDecisionId.New(),
                TripPlanId.New(),
                "item-1",
                TripItemDecisionStatus.Retained,
                "x",
                "owner-1",
                CreatedAtUtc));

        Assert.Equal(TripPlanErrorCodes.InvalidDecision, exception.Code);
    }

    private static TripItemDecision CreateDecision()
    {
        return TripItemDecision.Create(
            TripItemDecisionId.New(),
            TripPlanId.New(),
            "item-1",
            TripItemDecisionStatus.Review,
            "Le groupe doit encore en parler.",
            "owner-1",
            CreatedAtUtc);
    }
}
