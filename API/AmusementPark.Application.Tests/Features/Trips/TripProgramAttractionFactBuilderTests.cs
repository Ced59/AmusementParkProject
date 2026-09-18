using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripProgramAttractionFactBuilderTests
{
    [Fact]
    public void Build_ShouldKeepTheLatestOppositionDateAndTreatHiddenContentAsUnavailable()
    {
        DateTime decisionAtUtc = new DateTime(2027, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime preferenceAtUtc = decisionAtUtc.AddHours(1);
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc masqué",
            IsVisible = false,
            Status = ParkStatus.Operating,
        };
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = park.Id,
            Name = "Attraction interne",
            Category = ParkItemCategory.Attraction,
            IsVisible = true,
            AttractionDetails = new AttractionDetails { Status = "Operating" },
        };
        TripItemDecision decision = TripItemDecision.Create(
            TripItemDecisionId.New(),
            TripPlanId.New(),
            item.Id,
            TripItemDecisionStatus.Retained,
            "Le groupe souhaite la conserver.",
            "owner-1",
            decisionAtUtc);
        TripPreferenceCount opposition = new TripPreferenceCount(
            item.Id,
            TripItemPreferenceLevel.NotForMe,
            1,
            preferenceAtUtc);

        IReadOnlyCollection<TripProgramAttractionFact> facts = new TripProgramAttractionFactBuilder().Build(
            new[] { decision },
            new Dictionary<string, ParkItem>(StringComparer.Ordinal) { [item.Id] = item },
            new Dictionary<string, Park>(StringComparer.Ordinal) { [park.Id] = park },
            new[] { opposition });

        TripProgramAttractionFact fact = Assert.Single(facts);
        Assert.False(fact.IsAvailable);
        Assert.Equal(1, fact.NotForMeCount);
        Assert.Equal(preferenceAtUtc, fact.LatestNotForMeAtUtc);
    }
}
