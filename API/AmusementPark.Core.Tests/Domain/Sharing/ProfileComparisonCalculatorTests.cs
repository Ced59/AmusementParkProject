using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class ProfileComparisonCalculatorTests
{
    [Fact]
    public void Calculate_WithFourCommonRatings_ShouldNeverInventCorrelationOrPercentage()
    {
        ProfileComparisonPassportData creator = CreatePassport(
            "Camille",
            new[]
            {
                new ProfileComparisonParkData("Parc commun", "FR", 3),
                new ProfileComparisonParkData("Parc de Camille", "BE", 1),
            },
            CreateRatings(4, 0d));
        ProfileComparisonPassportData acceptor = CreatePassport(
            "Alex",
            new[]
            {
                new ProfileComparisonParkData("Parc commun", "fr", 2),
                new ProfileComparisonParkData("Parc d'Alex", "DE", 4),
            },
            CreateRatings(4, 0.5d));

        ProfileComparisonCalculation result = ProfileComparisonCalculator.Calculate(
            creator,
            acceptor,
            new[]
            {
                ProfileComparisonCategory.VisitedParks,
                ProfileComparisonCategory.PersonalRatings,
            });

        Assert.Equal(3, result.Parks.Count);
        Assert.Single(result.Parks, static park =>
            park.CreatorVisitCount.HasValue && park.AcceptorVisitCount.HasValue);
        Assert.Equal(4, result.CommonRatingCount);
        Assert.Equal(5, result.MinimumRatingsForCorrelation);
        Assert.Null(result.RatingCorrelation);
        Assert.All(result.Ratings, static rating =>
            Assert.Equal(ProfileComparisonRatingAffinity.Close, rating.Affinity));
    }

    [Fact]
    public void Calculate_WithFiveVariedRatings_ShouldExposeBoundedCorrelation()
    {
        ProfileComparisonPassportData creator = CreatePassport(
            "Camille",
            Array.Empty<ProfileComparisonParkData>(),
            CreateRatings(5, 0d));
        ProfileComparisonPassportData acceptor = CreatePassport(
            "Alex",
            Array.Empty<ProfileComparisonParkData>(),
            CreateRatings(5, 0d));

        ProfileComparisonCalculation result = ProfileComparisonCalculator.Calculate(
            creator,
            acceptor,
            new[] { ProfileComparisonCategory.PersonalRatings });

        Assert.Equal(1d, result.RatingCorrelation);
        Assert.Equal(ProfileComparisonCalculator.CalculationVersion, result.CalculationVersion);
    }

    [Fact]
    public void Calculate_ShouldClassifyCloseNeutralAndDivergentRatingsAtDocumentedThresholds()
    {
        ProfileComparisonPassportData creator = CreatePassport(
            "Camille",
            Array.Empty<ProfileComparisonParkData>(),
            new[]
            {
                new ProfileComparisonRatingData("ParkItem", "Proche", "Parc", null, 4d),
                new ProfileComparisonRatingData("ParkItem", "Nuancée", "Parc", null, 4d),
                new ProfileComparisonRatingData("ParkItem", "Divergente", "Parc", null, 4d),
            });
        ProfileComparisonPassportData acceptor = CreatePassport(
            "Alex",
            Array.Empty<ProfileComparisonParkData>(),
            new[]
            {
                new ProfileComparisonRatingData("ParkItem", "Proche", "Parc", null, 3.5d),
                new ProfileComparisonRatingData("ParkItem", "Nuancée", "Parc", null, 3d),
                new ProfileComparisonRatingData("ParkItem", "Divergente", "Parc", null, 2.5d),
            });

        ProfileComparisonCalculation result = ProfileComparisonCalculator.Calculate(
            creator,
            acceptor,
            new[] { ProfileComparisonCategory.PersonalRatings });

        Assert.Equal(
            ProfileComparisonRatingAffinity.Close,
            Assert.Single(result.Ratings, static rating => rating.Name == "Proche").Affinity);
        Assert.Equal(
            ProfileComparisonRatingAffinity.Neutral,
            Assert.Single(result.Ratings, static rating => rating.Name == "Nuancée").Affinity);
        Assert.Equal(
            ProfileComparisonRatingAffinity.Divergent,
            Assert.Single(result.Ratings, static rating => rating.Name == "Divergente").Affinity);
    }

    private static ProfileComparisonPassportData CreatePassport(
        string displayName,
        IReadOnlyCollection<ProfileComparisonParkData> parks,
        IReadOnlyCollection<ProfileComparisonRatingData> ratings)
    {
        return new ProfileComparisonPassportData(
            displayName,
            parks,
            ratings,
            Array.Empty<ProfileComparisonYearData>(),
            Array.Empty<ProfileComparisonMissedItemData>(),
            false);
    }

    private static ProfileComparisonRatingData[] CreateRatings(int count, double offset)
    {
        return Enumerable.Range(1, count)
            .Select(index => new ProfileComparisonRatingData(
                "ParkItem",
                $"Attraction {index}",
                "Parc commun",
                "Attraction",
                index + offset))
            .ToArray();
    }
}
