using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class ProfileComparisonCategoryPolicyTests
{
    [Fact]
    public void AllowsAll_ShouldRequireEveryPublicPassportField()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            new[]
            {
                ShareContentField.GeographicStatistics,
                ShareContentField.RideCount,
                ShareContentField.GlobalRatings,
            });

        Assert.True(ProfileComparisonCategoryPolicy.AllowsAll(
            policy,
            new[]
            {
                ProfileComparisonCategory.VisitedParks,
                ProfileComparisonCategory.PersonalRatings,
                ProfileComparisonCategory.YearlyActivity,
            }));
        Assert.False(ProfileComparisonCategoryPolicy.Allows(
            policy,
            ProfileComparisonCategory.MissedItems));
    }

    [Fact]
    public void Allows_ShouldRejectAnotherPublicationType()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.YearRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.GeographicStatistics });

        Assert.False(ProfileComparisonCategoryPolicy.Allows(
            policy,
            ProfileComparisonCategory.VisitedParks));
    }
}
