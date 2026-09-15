using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class UserCollectionEntryTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 15, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldNormalizePrivateStateAndRemainPrivateByDesign()
    {
        DateRangePreference preferredPeriod = new DateRangePreference(
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 30));

        UserCollectionEntry entry = UserCollectionEntry.Create(
            UserCollectionEntryId.Parse("entry-1"),
            " user-1 ",
            CollectionTargetType.Park,
            " park-1 ",
            UserCollectionKind.WantToVisit,
            CollectionTargetStatus.Available,
            "  À découvrir au printemps.  ",
            2,
            preferredPeriod,
            NowUtc);

        Assert.Equal("user-1", entry.UserId);
        Assert.Equal("park-1", entry.TargetId);
        Assert.Equal("À découvrir au printemps.", entry.PrivateNote);
        Assert.Equal(preferredPeriod, entry.PreferredPeriod);
        Assert.Equal(1, entry.Version);
        Assert.Equal(NowUtc, entry.CreatedAtUtc);
        Assert.Equal(NowUtc, entry.UpdatedAtUtc);
        Assert.DoesNotContain(
            entry.GetType().GetProperties(),
            property => property.Name.Contains("Public", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(CollectionTargetType.ParkItem, UserCollectionKind.WantToVisit)]
    [InlineData(CollectionTargetType.Park, UserCollectionKind.WantToExperience)]
    public void Create_WithIncompatibleIntentAndTarget_ShouldRejectEntry(
        CollectionTargetType targetType,
        UserCollectionKind kind)
    {
        UserCollectionValidationException exception = Assert.Throws<
            UserCollectionValidationException>(() => CreateEntry(targetType, kind));

        Assert.Equal(UserCollectionErrorCodes.IncompatibleTarget, exception.Code);
    }

    [Theory]
    [InlineData(CollectionTargetType.Park, UserCollectionKind.Favorite)]
    [InlineData(CollectionTargetType.ParkItem, UserCollectionKind.Favorite)]
    [InlineData(CollectionTargetType.Park, UserCollectionKind.WantToVisit)]
    [InlineData(CollectionTargetType.ParkItem, UserCollectionKind.WantToExperience)]
    [InlineData(CollectionTargetType.Park, UserCollectionKind.Planned)]
    [InlineData(CollectionTargetType.ParkItem, UserCollectionKind.Planned)]
    public void Create_WithCompatibleIntentAndTarget_ShouldCreateEntry(
        CollectionTargetType targetType,
        UserCollectionKind kind)
    {
        UserCollectionEntry entry = CreateEntry(targetType, kind);

        Assert.Equal(targetType, entry.TargetType);
        Assert.Equal(kind, entry.Kind);
    }

    [Fact]
    public void SameTarget_WithDifferentIntent_ShouldKeepDistinctLogicalIdentities()
    {
        UserCollectionEntry favorite = CreateEntry(
            CollectionTargetType.Park,
            UserCollectionKind.Favorite);
        UserCollectionEntry visitWish = UserCollectionEntry.Create(
            UserCollectionEntryId.Parse("entry-2"),
            "user-1",
            CollectionTargetType.Park,
            "target-1",
            UserCollectionKind.WantToVisit,
            CollectionTargetStatus.Available,
            null,
            null,
            null,
            NowUtc);

        Assert.False(favorite.HasSameLogicalIdentityAs(visitWish));
    }

    [Fact]
    public void DuplicateIntent_ShouldExposeSameLogicalIdentity()
    {
        UserCollectionEntry first = CreateEntry(
            CollectionTargetType.Park,
            UserCollectionKind.Favorite);
        UserCollectionEntry second = UserCollectionEntry.Create(
            UserCollectionEntryId.Parse("entry-2"),
            "user-1",
            CollectionTargetType.Park,
            "target-1",
            UserCollectionKind.Favorite,
            CollectionTargetStatus.Unknown,
            "Different private details",
            4,
            null,
            NowUtc);

        Assert.True(first.HasSameLogicalIdentityAs(second));
    }

    [Fact]
    public void SynchronizeTargetStatus_WhenTargetPermanentlyCloses_ShouldKeepIntent()
    {
        UserCollectionEntry entry = CreateEntry(
            CollectionTargetType.ParkItem,
            UserCollectionKind.WantToExperience);
        string originalTargetId = entry.TargetId;
        UserCollectionKind originalKind = entry.Kind;

        entry.SynchronizeTargetStatus(
            CollectionTargetStatus.PermanentlyClosed,
            NowUtc.AddMinutes(1));

        Assert.Equal(CollectionTargetStatus.PermanentlyClosed, entry.TargetStatus);
        Assert.Equal(originalTargetId, entry.TargetId);
        Assert.Equal(originalKind, entry.Kind);
        Assert.Equal(2, entry.Version);
    }

    [Fact]
    public void UpdatePreferences_WhenValuesDoNotChange_ShouldRemainIdempotent()
    {
        UserCollectionEntry entry = CreateEntry(
            CollectionTargetType.Park,
            UserCollectionKind.Favorite);

        entry.UpdatePreferences(null, null, null, NowUtc.AddMinutes(1));

        Assert.Equal(1, entry.Version);
        Assert.Equal(NowUtc, entry.UpdatedAtUtc);
    }

    [Fact]
    public void UpdatePreferences_ShouldReplaceOnlyPrivatePlanningDetails()
    {
        UserCollectionEntry entry = CreateEntry(
            CollectionTargetType.Park,
            UserCollectionKind.Planned);
        DateRangePreference preferredPeriod = new DateRangePreference(
            new DateOnly(2027, 8, 1),
            null);

        entry.UpdatePreferences(
            "  Vacances d'été  ",
            UserCollectionEntry.MaximumPriority,
            preferredPeriod,
            NowUtc.AddMinutes(1));

        Assert.Equal("Vacances d'été", entry.PrivateNote);
        Assert.Equal(UserCollectionEntry.MaximumPriority, entry.Priority);
        Assert.Equal(preferredPeriod, entry.PreferredPeriod);
        Assert.Equal(2, entry.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_WithOutOfRangePriority_ShouldRejectEntry(int priority)
    {
        UserCollectionValidationException exception = Assert.Throws<
            UserCollectionValidationException>(() => UserCollectionEntry.Create(
                UserCollectionEntryId.Parse("entry-1"),
                "user-1",
                CollectionTargetType.Park,
                "park-1",
                UserCollectionKind.Favorite,
                CollectionTargetStatus.Available,
                null,
                priority,
                null,
                NowUtc));

        Assert.Equal(UserCollectionErrorCodes.InvalidPriority, exception.Code);
    }

    [Fact]
    public void Restore_WithInvalidTimestampOrder_ShouldRejectState()
    {
        UserCollectionValidationException exception = Assert.Throws<
            UserCollectionValidationException>(() => UserCollectionEntry.Restore(
                UserCollectionEntryId.Parse("entry-1"),
                "user-1",
                CollectionTargetType.Park,
                "park-1",
                UserCollectionKind.Favorite,
                CollectionTargetStatus.Available,
                null,
                null,
                null,
                NowUtc,
                NowUtc.AddMinutes(-1),
                1));

        Assert.Equal(UserCollectionErrorCodes.InvalidTimestamp, exception.Code);
    }

    [Fact]
    public void SynchronizeTargetStatus_WithOlderTimestamp_ShouldRejectMutation()
    {
        UserCollectionEntry entry = CreateEntry(
            CollectionTargetType.Park,
            UserCollectionKind.Favorite);

        UserCollectionValidationException exception = Assert.Throws<
            UserCollectionValidationException>(() => entry.SynchronizeTargetStatus(
                CollectionTargetStatus.TemporarilyClosed,
                NowUtc.AddTicks(-1)));

        Assert.Equal(UserCollectionErrorCodes.InvalidTimestamp, exception.Code);
    }

    private static UserCollectionEntry CreateEntry(
        CollectionTargetType targetType,
        UserCollectionKind kind)
    {
        return UserCollectionEntry.Create(
            UserCollectionEntryId.Parse("entry-1"),
            "user-1",
            targetType,
            "target-1",
            kind,
            CollectionTargetStatus.Available,
            null,
            null,
            null,
            NowUtc);
    }
}
