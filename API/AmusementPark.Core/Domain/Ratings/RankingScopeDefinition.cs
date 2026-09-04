using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Ratings;

public enum RankingTargetFamily
{
    Parks,
    ParkItems,
}

public enum RankingScopeFilterKind
{
    Global,
    ParkItemCategory,
}

public enum RankingPublicationMode
{
    DurableSnapshot,
}

/// <summary>
/// Définition immuable d'un classement pouvant être matérialisé.
/// </summary>
public sealed class RankingScopeDefinition
{
    public const int MinimumPageSize = 250;

    public const int MaximumPageSize = 500;

    public RankingScopeDefinition(
        RankingScopeKey key,
        RankingTargetFamily targetFamily,
        RankingFilterDefinition filter,
        bool isPublic,
        RatingMethodologyVersion methodologyVersion,
        int minimumEligibleEntries,
        int pageSize,
        decimal scoreTieEpsilon,
        RankingPublicationMode publicationMode)
    {
        _ = key.Value;
        if (!Enum.IsDefined(targetFamily))
        {
            throw new ArgumentOutOfRangeException(nameof(targetFamily));
        }

        ArgumentNullException.ThrowIfNull(filter);
        ValidateFilterCompatibility(targetFamily, filter);
        ValidateKeyCompatibility(key, targetFamily, filter);
        _ = methodologyVersion.Value;

        if (minimumEligibleEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumEligibleEntries));
        }

        if (pageSize < MinimumPageSize || pageSize > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        if (scoreTieEpsilon <= 0m || scoreTieEpsilon > 0.1m)
        {
            throw new ArgumentOutOfRangeException(nameof(scoreTieEpsilon));
        }

        if (!Enum.IsDefined(publicationMode))
        {
            throw new ArgumentOutOfRangeException(nameof(publicationMode));
        }

        this.Key = key;
        this.TargetFamily = targetFamily;
        this.Filter = filter;
        this.IsPublic = isPublic;
        this.MethodologyVersion = methodologyVersion;
        this.MinimumEligibleEntries = minimumEligibleEntries;
        this.PageSize = pageSize;
        this.ScoreTieEpsilon = scoreTieEpsilon;
        this.PublicationMode = publicationMode;
    }

    public RankingScopeKey Key { get; }

    public RankingTargetFamily TargetFamily { get; }

    public RankingFilterDefinition Filter { get; }

    public bool IsPublic { get; }

    public RatingMethodologyVersion MethodologyVersion { get; }

    public int MinimumEligibleEntries { get; }

    public int PageSize { get; }

    public decimal ScoreTieEpsilon { get; }

    public RankingPublicationMode PublicationMode { get; }

    public RankingPublicationEligibility EvaluatePublication(int eligibleEntryCount)
    {
        if (eligibleEntryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eligibleEntryCount));
        }

        return eligibleEntryCount >= this.MinimumEligibleEntries
            ? new RankingPublicationEligibility(true, null)
            : new RankingPublicationEligibility(false, RankingIneligibilityReason.TooFewComparableEntries);
    }

    public bool AreScoresTied(double leftScore, double rightScore)
    {
        ValidateScore(leftScore, nameof(leftScore));
        ValidateScore(rightScore, nameof(rightScore));
        decimal difference = Math.Abs((decimal)leftScore - (decimal)rightScore);
        return difference < this.ScoreTieEpsilon;
    }

    public bool AcceptsTarget(RatingTargetType targetType, ParkItemCategory? parkItemCategory)
    {
        if (!Enum.IsDefined(targetType))
        {
            return false;
        }

        return this.TargetFamily switch
        {
            RankingTargetFamily.Parks => targetType == RatingTargetType.Park &&
                !parkItemCategory.HasValue,
            RankingTargetFamily.ParkItems => targetType == RatingTargetType.ParkItem &&
                parkItemCategory == this.Filter.ParkItemCategory,
            _ => false,
        };
    }

    /// <summary>
    /// Indique si une mutation de note peut modifier le contenu de ce scope.
    /// Une note d'élément influe aussi sur les classements de parcs, dont le score est composé.
    /// </summary>
    public bool IsAffectedByRatingMutation(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory)
    {
        if (!Enum.IsDefined(targetType))
        {
            return false;
        }

        if (targetType == RatingTargetType.Park)
        {
            return !parkItemCategory.HasValue && this.TargetFamily == RankingTargetFamily.Parks;
        }

        if (this.TargetFamily == RankingTargetFamily.Parks)
        {
            return true;
        }

        if (!parkItemCategory.HasValue || !Enum.IsDefined(parkItemCategory.Value))
        {
            return false;
        }

        return this.AcceptsTarget(targetType, parkItemCategory);
    }

    private static void ValidateFilterCompatibility(
        RankingTargetFamily targetFamily,
        RankingFilterDefinition filter)
    {
        bool isCompatible = targetFamily switch
        {
            RankingTargetFamily.Parks => filter.Kind == RankingScopeFilterKind.Global
                && !filter.ParkItemCategory.HasValue,
            RankingTargetFamily.ParkItems => filter.Kind == RankingScopeFilterKind.ParkItemCategory
                && filter.ParkItemCategory.HasValue,
            _ => false,
        };

        if (!isCompatible)
        {
            throw new ArgumentException(
                "The ranking filter is incompatible with its target family.",
                nameof(filter));
        }
    }

    private static void ValidateKeyCompatibility(
        RankingScopeKey key,
        RankingTargetFamily targetFamily,
        RankingFilterDefinition filter)
    {
        string expectedKey = targetFamily switch
        {
            RankingTargetFamily.Parks => "parks:global",
            RankingTargetFamily.ParkItems =>
                $"park-items:category:{ResolveCategoryKey(filter.ParkItemCategory!.Value)}",
            _ => string.Empty,
        };

        if (!string.Equals(key.Value, expectedKey, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The ranking scope key does not match its target family and filter.",
                nameof(key));
        }
    }

    private static string ResolveCategoryKey(ParkItemCategory category)
    {
        return category switch
        {
            ParkItemCategory.Attraction => "attraction",
            ParkItemCategory.Restaurant => "restaurant",
            ParkItemCategory.Hotel => "hotel",
            ParkItemCategory.Animal => "animal",
            ParkItemCategory.Show => "show",
            ParkItemCategory.Shop => "shop",
            ParkItemCategory.Service => "service",
            ParkItemCategory.Transport => "transport",
            ParkItemCategory.Other => "other",
            _ => throw new ArgumentOutOfRangeException(nameof(category)),
        };
    }

    private static void ValidateScore(double score, string parameterName)
    {
        if (!double.IsFinite(score) ||
            score < RatingValue.MinimumHalfSteps / 2d ||
            score > RatingValue.MaximumHalfSteps / 2d)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
