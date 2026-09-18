using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripItemPreferenceTests
{
    private static readonly DateTime CreatedAtUtc = new(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Set_ShouldIncrementVersionOnlyWhenTheChoiceChanges()
    {
        TripItemPreference preference = CreatePreference(
            TripItemPreferenceLevel.WantToDo,
            TripItemPreferenceReason.Sensations);

        preference.Set(
            TripItemPreferenceLevel.WantToDo,
            TripItemPreferenceReason.Sensations,
            CreatedAtUtc.AddMinutes(1));
        Assert.Equal(1, preference.Version);

        preference.Set(
            TripItemPreferenceLevel.MustDo,
            TripItemPreferenceReason.AlreadyDone,
            CreatedAtUtc.AddMinutes(2));

        Assert.Equal(2, preference.Version);
        Assert.Equal(TripItemPreferenceLevel.MustDo, preference.Level);
        Assert.Equal(TripItemPreferenceReason.AlreadyDone, preference.Reason);
    }

    [Fact]
    public void Set_WhenChoiceReturnsToUnknown_ShouldRemoveTheReason()
    {
        TripItemPreference preference = CreatePreference(
            TripItemPreferenceLevel.NotForMe,
            TripItemPreferenceReason.Height);

        preference.Set(TripItemPreferenceLevel.Unknown, null, CreatedAtUtc.AddMinutes(1));

        Assert.Equal(TripItemPreferenceLevel.Unknown, preference.Level);
        Assert.Null(preference.Reason);
    }

    [Fact]
    public void Create_WhenUnknownHasAReason_ShouldRejectTheInvalidCombination()
    {
        TripPlanValidationException exception = Assert.Throws<TripPlanValidationException>(() =>
            CreatePreference(TripItemPreferenceLevel.Unknown, TripItemPreferenceReason.Other));

        Assert.Equal(TripPlanErrorCodes.InvalidPreference, exception.Code);
    }

    private static TripItemPreference CreatePreference(
        TripItemPreferenceLevel level,
        TripItemPreferenceReason? reason)
    {
        return TripItemPreference.Create(
            TripItemPreferenceId.New(),
            TripPlanId.New(),
            TripMemberId.New(),
            "user-1",
            "item-1",
            level,
            reason,
            CreatedAtUtc);
    }
}
