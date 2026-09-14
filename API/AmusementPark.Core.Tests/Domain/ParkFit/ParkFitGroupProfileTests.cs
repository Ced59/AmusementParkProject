using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitGroupProfileTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 14, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldNormalizeAliasAndKeepOnlyUsefulFacts()
    {
        ParkFitGroupProfile profile = ParkFitGroupProfile.Create(
            ParkFitGroupProfileId.Parse("profile-1"),
            " user-1 ",
            "  Enfant   8 ans ",
            126,
            8,
            true,
            38,
            NowUtc);

        Assert.Equal("user-1", profile.OwnerUserId);
        Assert.Equal("Enfant 8 ans", profile.Alias);
        Assert.Equal("ENFANT 8 ANS", profile.NormalizedAlias);
        Assert.Equal(126, profile.HeightCentimeters);
        Assert.Equal(8, profile.AgeYears);
        Assert.Equal(38, profile.CompanionAgeYears);
        Assert.Equal(1, profile.Version);
    }

    [Fact]
    public void Update_WhenAccompanimentIsDisabled_ShouldForgetCompanionAge()
    {
        ParkFitGroupProfile profile = CreateProfile();

        profile.Update("Alex", 171, 31, false, null, NowUtc.AddMinutes(1));

        Assert.False(profile.CanBeAccompanied);
        Assert.Null(profile.CompanionAgeYears);
        Assert.Equal(2, profile.Version);
        Assert.Equal(NowUtc.AddMinutes(1), profile.UpdatedAtUtc);
    }

    [Fact]
    public void Update_WithCompanionAgeButNoAccompaniment_ShouldRejectFacts()
    {
        ParkFitGroupProfile profile = CreateProfile();

        ParkFitGroupProfileValidationException exception = Assert.Throws<
            ParkFitGroupProfileValidationException>(() => profile.Update(
                "Alex",
                171,
                31,
                false,
                40,
                NowUtc.AddMinutes(1)));

        Assert.Equal(ParkFitGroupProfileErrorCodes.InvalidFacts, exception.Code);
    }

    private static ParkFitGroupProfile CreateProfile()
    {
        return ParkFitGroupProfile.Create(
            ParkFitGroupProfileId.Parse("profile-1"),
            "user-1",
            "Alex",
            170,
            30,
            true,
            40,
            NowUtc);
    }
}
