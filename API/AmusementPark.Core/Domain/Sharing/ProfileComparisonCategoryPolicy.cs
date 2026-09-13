namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Relie chaque rubrique de comparaison à la liste blanche du passeport public.
/// </summary>
public static class ProfileComparisonCategoryPolicy
{
    public static bool Allows(
        ShareContentPolicy contentPolicy,
        ProfileComparisonCategory category)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        if (contentPolicy.PublicationType != SharePublicationType.PassportProfile
            || !Enum.IsDefined(category))
        {
            return false;
        }

        return category switch
        {
            ProfileComparisonCategory.VisitedParks =>
                contentPolicy.Includes(ShareContentField.GeographicStatistics),
            ProfileComparisonCategory.PersonalRatings =>
                contentPolicy.Includes(ShareContentField.GlobalRatings),
            ProfileComparisonCategory.YearlyActivity =>
                contentPolicy.Includes(ShareContentField.GeographicStatistics)
                && contentPolicy.Includes(ShareContentField.RideCount),
            ProfileComparisonCategory.MissedItems =>
                contentPolicy.Includes(ShareContentField.MissedItems),
            _ => false,
        };
    }

    public static bool AllowsAll(
        ShareContentPolicy contentPolicy,
        IEnumerable<ProfileComparisonCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        return categories.All(category => Allows(contentPolicy, category));
    }
}
