namespace AmusementPark.Core.Domain.Sharing;

public static class ProfileComparisonCalculator
{
    public const int MinimumRatingsForCorrelation = 5;
    public const string CalculationVersion = "profile-comparison-v1";

    private const double CloseDifferenceMaximum = 0.5d;
    private const double DivergentDifferenceMinimum = 1.5d;

    public static ProfileComparisonCalculation Calculate(
        ProfileComparisonPassportData creator,
        ProfileComparisonPassportData acceptor,
        IEnumerable<ProfileComparisonCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(creator);
        ArgumentNullException.ThrowIfNull(acceptor);
        ArgumentNullException.ThrowIfNull(categories);
        ProfileComparisonCategory[] selectedCategories = categories
            .Distinct()
            .OrderBy(static category => category)
            .ToArray();
        if (selectedCategories.Length == 0
            || selectedCategories.Any(static category => !Enum.IsDefined(category)))
        {
            throw new ArgumentException("At least one valid comparison category is required.", nameof(categories));
        }

        ProfileComparisonParkResult[] parks = selectedCategories.Contains(
                ProfileComparisonCategory.VisitedParks)
            ? CompareParks(creator.Parks, acceptor.Parks)
            : Array.Empty<ProfileComparisonParkResult>();
        ProfileComparisonRatingResult[] ratings = selectedCategories.Contains(
                ProfileComparisonCategory.PersonalRatings)
            ? CompareRatings(creator.Ratings, acceptor.Ratings)
            : Array.Empty<ProfileComparisonRatingResult>();
        ProfileComparisonYearResult[] years = selectedCategories.Contains(
                ProfileComparisonCategory.YearlyActivity)
            ? CompareYears(creator.Years, acceptor.Years)
            : Array.Empty<ProfileComparisonYearResult>();
        ProfileComparisonMissedItemResult[] missedItems = selectedCategories.Contains(
                ProfileComparisonCategory.MissedItems)
            ? CompareMissedItems(creator.MissedItems, acceptor.MissedItems)
            : Array.Empty<ProfileComparisonMissedItemResult>();

        return new ProfileComparisonCalculation(
            NormalizeOptional(creator.DisplayName),
            NormalizeOptional(acceptor.DisplayName),
            selectedCategories,
            parks,
            ratings,
            years,
            missedItems,
            ratings.Length,
            MinimumRatingsForCorrelation,
            CalculateCorrelation(ratings),
            creator.HasIncompleteCatalog || acceptor.HasIncompleteCatalog,
            CalculationVersion);
    }

    private static ProfileComparisonParkResult[] CompareParks(
        IEnumerable<ProfileComparisonParkData> creator,
        IEnumerable<ProfileComparisonParkData> acceptor)
    {
        IReadOnlyDictionary<string, ProfileComparisonParkData> creatorByKey =
            IndexBy(creator, static park => CreateKey(park.CountryCode, park.Name));
        IReadOnlyDictionary<string, ProfileComparisonParkData> acceptorByKey =
            IndexBy(acceptor, static park => CreateKey(park.CountryCode, park.Name));
        return creatorByKey.Keys
            .Concat(acceptorByKey.Keys)
            .Distinct(StringComparer.Ordinal)
            .Select(key =>
            {
                creatorByKey.TryGetValue(key, out ProfileComparisonParkData? creatorPark);
                acceptorByKey.TryGetValue(key, out ProfileComparisonParkData? acceptorPark);
                ProfileComparisonParkData visible = creatorPark ?? acceptorPark!;
                return new ProfileComparisonParkResult(
                    visible.Name.Trim(),
                    NormalizeCountryCode(visible.CountryCode),
                    creatorPark?.VisitCount,
                    acceptorPark?.VisitCount);
            })
            .OrderByDescending(static park => park.CreatorVisitCount.HasValue
                && park.AcceptorVisitCount.HasValue)
            .ThenBy(static park => park.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ProfileComparisonRatingResult[] CompareRatings(
        IEnumerable<ProfileComparisonRatingData> creator,
        IEnumerable<ProfileComparisonRatingData> acceptor)
    {
        IReadOnlyDictionary<string, ProfileComparisonRatingData> creatorByKey =
            IndexBy(creator, static rating => CreateKey(
                rating.TargetType,
                rating.ParkName,
                rating.Name));
        IReadOnlyDictionary<string, ProfileComparisonRatingData> acceptorByKey =
            IndexBy(acceptor, static rating => CreateKey(
                rating.TargetType,
                rating.ParkName,
                rating.Name));
        return creatorByKey.Keys
            .Intersect(acceptorByKey.Keys, StringComparer.Ordinal)
            .Select(key =>
            {
                ProfileComparisonRatingData creatorRating = creatorByKey[key];
                ProfileComparisonRatingData acceptorRating = acceptorByKey[key];
                double difference = Math.Round(
                    Math.Abs(creatorRating.Rating - acceptorRating.Rating),
                    2,
                    MidpointRounding.AwayFromZero);
                return new ProfileComparisonRatingResult(
                    creatorRating.TargetType.Trim(),
                    creatorRating.Name.Trim(),
                    NormalizeOptional(creatorRating.ParkName),
                    NormalizeOptional(creatorRating.Category),
                    creatorRating.Rating,
                    acceptorRating.Rating,
                    difference,
                    difference <= CloseDifferenceMaximum
                        ? ProfileComparisonRatingAffinity.Close
                        : difference >= DivergentDifferenceMinimum
                            ? ProfileComparisonRatingAffinity.Divergent
                            : ProfileComparisonRatingAffinity.Neutral);
            })
            .OrderByDescending(static rating => rating.AbsoluteDifference)
            .ThenBy(static rating => rating.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ProfileComparisonYearResult[] CompareYears(
        IEnumerable<ProfileComparisonYearData> creator,
        IEnumerable<ProfileComparisonYearData> acceptor)
    {
        IReadOnlyDictionary<int, ProfileComparisonYearData> creatorByYear = creator
            .GroupBy(static year => year.Year)
            .ToDictionary(static group => group.Key, static group => group.First());
        IReadOnlyDictionary<int, ProfileComparisonYearData> acceptorByYear = acceptor
            .GroupBy(static year => year.Year)
            .ToDictionary(static group => group.Key, static group => group.First());
        return creatorByYear.Keys
            .Intersect(acceptorByYear.Keys)
            .OrderByDescending(static year => year)
            .Select(year => new ProfileComparisonYearResult(
                year,
                creatorByYear[year].VisitCount,
                acceptorByYear[year].VisitCount,
                creatorByYear[year].RideCount,
                acceptorByYear[year].RideCount))
            .ToArray();
    }

    private static ProfileComparisonMissedItemResult[] CompareMissedItems(
        IEnumerable<ProfileComparisonMissedItemData> creator,
        IEnumerable<ProfileComparisonMissedItemData> acceptor)
    {
        IReadOnlyDictionary<string, ProfileComparisonMissedItemData> creatorByKey =
            IndexBy(creator, static item => CreateKey(item.Status, item.Name));
        IReadOnlyDictionary<string, ProfileComparisonMissedItemData> acceptorByKey =
            IndexBy(acceptor, static item => CreateKey(item.Status, item.Name));
        return creatorByKey.Keys
            .Intersect(acceptorByKey.Keys, StringComparer.Ordinal)
            .Select(key => new ProfileComparisonMissedItemResult(
                creatorByKey[key].Name.Trim(),
                creatorByKey[key].Status.Trim(),
                creatorByKey[key].OccurrenceCount,
                acceptorByKey[key].OccurrenceCount))
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static double? CalculateCorrelation(
        IReadOnlyCollection<ProfileComparisonRatingResult> ratings)
    {
        if (ratings.Count < MinimumRatingsForCorrelation)
        {
            return null;
        }

        double creatorAverage = ratings.Average(static rating => rating.CreatorRating);
        double acceptorAverage = ratings.Average(static rating => rating.AcceptorRating);
        double covariance = ratings.Sum(rating =>
            (rating.CreatorRating - creatorAverage) * (rating.AcceptorRating - acceptorAverage));
        double creatorVariance = ratings.Sum(rating =>
            Math.Pow(rating.CreatorRating - creatorAverage, 2));
        double acceptorVariance = ratings.Sum(rating =>
            Math.Pow(rating.AcceptorRating - acceptorAverage, 2));
        double denominator = Math.Sqrt(creatorVariance * acceptorVariance);
        return denominator <= double.Epsilon
            ? null
            : Math.Round(covariance / denominator, 3, MidpointRounding.AwayFromZero);
    }

    private static IReadOnlyDictionary<string, TValue> IndexBy<TValue>(
        IEnumerable<TValue> values,
        Func<TValue, string> keySelector)
    {
        return (values ?? Array.Empty<TValue>())
            .GroupBy(keySelector, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First(),
                StringComparer.Ordinal);
    }

    private static string CreateKey(params string?[] parts)
    {
        return string.Join(
            "\u001f",
            parts.Select(static part => (part ?? string.Empty).Trim().ToUpperInvariant()));
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? NormalizeCountryCode(string? value)
    {
        string normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.Length == 2 ? normalized : null;
    }
}
