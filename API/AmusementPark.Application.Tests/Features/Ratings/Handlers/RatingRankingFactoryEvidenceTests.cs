using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class RatingRankingFactoryEvidenceTests
{
    [Fact]
    public void BuildParkRankings_WhenPublicInventoryContainsUnratedItems_ShouldUseThemForCategoryCoverage()
    {
        IReadOnlyCollection<RatingRankingItemResult> sources = CreateSources();
        ParkRankingEvidenceFactsBatch facts = new ParkRankingEvidenceFactsBatch(
            new[]
            {
                new ParkRankingContributorFacts(
                    "park-1",
                    UniqueContributorCount: 15,
                    RatingObservationCount: 60,
                    DirectParkContributorCount: 10,
                    ItemContributorCount: 12),
            },
            new[]
            {
                CreatePublicItem("attraction-1", ParkItemCategory.Attraction),
                CreatePublicItem("attraction-2", ParkItemCategory.Attraction),
                CreatePublicItem("attraction-3", ParkItemCategory.Attraction),
                CreatePublicItem("attraction-unrated", ParkItemCategory.Attraction),
                CreatePublicItem("restaurant-1", ParkItemCategory.Restaurant),
                CreatePublicItem("restaurant-unrated", ParkItemCategory.Restaurant),
                CreatePublicItem("shop-1", ParkItemCategory.Shop),
            });

        ParkRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkRankings(sources, evidenceFacts: facts));

        Assert.Equal(15, ranking.UniqueContributorCount);
        Assert.Equal(60, ranking.RatingObservationCount);
        Assert.Equal(5, ranking.Evidence?.EligibleItemCount);
        Assert.Equal(2, ranking.Evidence?.EligibleCategoryCount);
        Assert.True(ranking.Evidence?.IsEligibleForMainRanking);
    }

    [Fact]
    public void BuildParkRankings_WhenAggregateObservationCountDiffers_ShouldExposeIntegrityExclusion()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.Park,
            "park-1",
            null,
            ratingCount: 10);
        ParkRankingEvidenceFactsBatch facts = new ParkRankingEvidenceFactsBatch(
            new[]
            {
                new ParkRankingContributorFacts(
                    "park-1",
                    UniqueContributorCount: 9,
                    RatingObservationCount: 9,
                    DirectParkContributorCount: 9,
                    ItemContributorCount: 0),
            },
            Array.Empty<PublicParkItemEvidenceFact>());

        ParkRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkRankings(new[] { source }, evidenceFacts: facts));

        Assert.Equal(RankingEvidenceLevel.Excluded, ranking.Evidence?.Level);
        Assert.False(ranking.Evidence?.IsEligibleForMainRanking);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            ranking.Evidence?.IneligibilityReason);
        Assert.Null(ranking.Evidence?.NextThreshold);
    }

    [Fact]
    public void BuildParkRankings_WhenPersistedAggregateIsStale_ShouldExposeIntegrityExclusion()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.Park,
            "park-1",
            null,
            ratingCount: 10) with
        {
            AggregateIntegrityIsValid = false,
        };
        ParkRankingEvidenceFactsBatch facts = new ParkRankingEvidenceFactsBatch(
            new[]
            {
                new ParkRankingContributorFacts(
                    "park-1",
                    UniqueContributorCount: 10,
                    RatingObservationCount: 10,
                    DirectParkContributorCount: 10,
                    ItemContributorCount: 0),
            },
            Array.Empty<PublicParkItemEvidenceFact>());

        ParkRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkRankings(new[] { source }, evidenceFacts: facts));

        Assert.Equal(RankingEvidenceLevel.Excluded, ranking.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            ranking.Evidence?.IneligibilityReason);
    }

    [Fact]
    public void BuildParkRankings_WhenDirectObservationsContainDuplicates_ShouldWithholdInvalidEvidence()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.Park,
            "park-1",
            null,
            ratingCount: 10) with
        {
            UniqueContributorCount = 5,
        };
        ParkRankingEvidenceFactsBatch facts = new ParkRankingEvidenceFactsBatch(
            new[]
            {
                new ParkRankingContributorFacts(
                    "park-1",
                    UniqueContributorCount: 5,
                    RatingObservationCount: 10,
                    DirectParkContributorCount: 5,
                    ItemContributorCount: 0),
            },
            Array.Empty<PublicParkItemEvidenceFact>());

        ParkRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkRankings(new[] { source }, evidenceFacts: facts));

        Assert.Null(ranking.Evidence);
        Assert.Null(ranking.UniqueContributorCount);
        Assert.Equal(10, ranking.RatingObservationCount);
    }

    [Fact]
    public void BuildParkItemRankings_WhenLegacyCountIsOutsideDomainRange_ShouldPreserveRankingWithoutEvidence()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.ParkItem,
            "attraction-invalid",
            ParkItemCategory.Attraction,
            ratingCount: (long)int.MaxValue + 1);

        ParkItemRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkItemRankings(new[] { source }));

        Assert.Equal((long)int.MaxValue + 1, ranking.RatingCount);
        Assert.Null(ranking.Evidence);
        Assert.Null(ranking.UniqueContributorCount);
    }

    [Fact]
    public void BuildParkItemRankings_WhenAggregateIsStale_ShouldExposeIntegrityExclusion()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.ParkItem,
            "attraction-stale",
            ParkItemCategory.Attraction,
            ratingCount: 10) with
        {
            AggregateIntegrityIsValid = false,
        };

        ParkItemRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkItemRankings(new[] { source }));

        Assert.Equal(RankingEvidenceLevel.Excluded, ranking.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            ranking.Evidence?.IneligibilityReason);
    }

    [Fact]
    public void BuildParkItemRankings_WhenAggregateIntegrityIsUnknown_ShouldWithholdEvidence()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.ParkItem,
            "attraction-unknown",
            ParkItemCategory.Attraction,
            ratingCount: 10) with
        {
            AggregateIntegrityIsValid = null,
        };

        ParkItemRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkItemRankings(new[] { source }));

        Assert.Null(ranking.Evidence);
    }

    [Fact]
    public void BuildParkItemRankings_WhenObservationsContainDuplicateContributors_ShouldUseDistinctCount()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.ParkItem,
            "attraction-duplicates",
            ParkItemCategory.Attraction,
            ratingCount: 10) with
        {
            UniqueContributorCount = 5,
        };

        ParkItemRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkItemRankings(new[] { source }));

        Assert.Equal(5, ranking.UniqueContributorCount);
        Assert.Equal(10, ranking.RatingObservationCount);
        Assert.Equal(RankingEvidenceLevel.Excluded, ranking.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            ranking.Evidence?.IneligibilityReason);
    }

    [Fact]
    public void ApplyParkItemEvidence_WhenPersistedScoreDivergesFromSource_ShouldExcludeAggregate()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.ParkItem,
            "attraction-corrupted",
            ParkItemCategory.Attraction,
            ratingCount: 10);
        IReadOnlyCollection<ParkItemRatingRankingResult> rankings =
            RatingRankingFactory.BuildParkItemRankings(new[] { source });
        IReadOnlyCollection<RatingAggregateSourceFact> sourceFacts = new[]
        {
            new RatingAggregateSourceFact(
                RatingTargetType.ParkItem,
                "attraction-corrupted",
                UniqueContributorCount: 10,
                RatingObservationCount: 10,
                RatingSum: 45d),
        };

        ParkItemRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.ApplyParkItemEvidence(rankings, new[] { source }, sourceFacts));

        Assert.Equal(RankingEvidenceLevel.Excluded, ranking.Evidence?.Level);
        Assert.Equal(
            RankingIneligibilityReason.AggregateIntegrityFailure,
            ranking.Evidence?.IneligibilityReason);
    }

    [Fact]
    public void BuildParkRankings_WhenPublicInventoryIsIncomplete_ShouldWithholdEvidence()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.Park,
            "park-1",
            null,
            ratingCount: 10);
        ParkRankingEvidenceFactsBatch evidenceFacts = new ParkRankingEvidenceFactsBatch(
            new[]
            {
                new ParkRankingContributorFacts(
                    "park-1",
                    UniqueContributorCount: 10,
                    RatingObservationCount: 10,
                    DirectParkContributorCount: 10,
                    ItemContributorCount: 0),
            },
            Array.Empty<PublicParkItemEvidenceFact>(),
            new[]
            {
                new RatingAggregateSourceFact(
                    RatingTargetType.Park,
                    "park-1",
                    UniqueContributorCount: 10,
                    RatingObservationCount: 10,
                    RatingSum: 45d),
            },
            new[] { "park-1" },
            AggregateSourceFactsWereRead: true);

        ParkRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkRankings(new[] { source }, evidenceFacts: evidenceFacts));

        Assert.Null(ranking.Evidence);
    }

    [Fact]
    public void BuildParkRankings_WhenAggregateMatchesVerifiedSource_ShouldKeepEvidenceEligible()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.Park,
            "park-1",
            null,
            ratingCount: 10) with
        {
            BayesianScore = 4d,
        };
        ParkRankingEvidenceFactsBatch evidenceFacts = new ParkRankingEvidenceFactsBatch(
            new[]
            {
                new ParkRankingContributorFacts(
                    "park-1",
                    UniqueContributorCount: 10,
                    RatingObservationCount: 10,
                    DirectParkContributorCount: 10,
                    ItemContributorCount: 0),
            },
            Array.Empty<PublicParkItemEvidenceFact>(),
            new[]
            {
                new RatingAggregateSourceFact(
                    RatingTargetType.Park,
                    "park-1",
                    UniqueContributorCount: 10,
                    RatingObservationCount: 10,
                    RatingSum: 45d),
            },
            Array.Empty<string>(),
            AggregateSourceFactsWereRead: true);

        ParkRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkRankings(new[] { source }, evidenceFacts: evidenceFacts));

        Assert.Equal(RankingEvidenceLevel.Eligible, ranking.Evidence?.Level);
    }

    [Fact]
    public void BuildParkRankings_WhenContributorFactsAreDuplicated_ShouldPreserveRankingWithoutEvidence()
    {
        RatingRankingItemResult source = CreateSource(
            RatingTargetType.Park,
            "park-1",
            null,
            ratingCount: 10);
        ParkRankingContributorFacts facts = new ParkRankingContributorFacts(
            "park-1",
            UniqueContributorCount: 10,
            RatingObservationCount: 10,
            DirectParkContributorCount: 10,
            ItemContributorCount: 0);
        ParkRankingEvidenceFactsBatch evidenceFacts = new ParkRankingEvidenceFactsBatch(
            new[] { facts, facts },
            Array.Empty<PublicParkItemEvidenceFact>());

        ParkRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkRankings(new[] { source }, evidenceFacts: evidenceFacts));

        Assert.Equal(1, ranking.Rank);
        Assert.Null(ranking.Evidence);
    }

    [Fact]
    public void BuildParkRankings_WhenOnlyItemComponentIsEligible_ShouldExposeProvisionalEvidence()
    {
        IReadOnlyCollection<RatingRankingItemResult> sources = CreateSources()
            .Where(static source => source.TargetType == RatingTargetType.ParkItem)
            .ToList();
        ParkRankingEvidenceFactsBatch evidenceFacts = new ParkRankingEvidenceFactsBatch(
            new[]
            {
                new ParkRankingContributorFacts(
                    "park-1",
                    UniqueContributorCount: 12,
                    RatingObservationCount: 50,
                    DirectParkContributorCount: 0,
                    ItemContributorCount: 12),
            },
            new[]
            {
                CreatePublicItem("attraction-1", ParkItemCategory.Attraction),
                CreatePublicItem("attraction-2", ParkItemCategory.Attraction),
                CreatePublicItem("attraction-3", ParkItemCategory.Attraction),
                CreatePublicItem("restaurant-1", ParkItemCategory.Restaurant),
                CreatePublicItem("shop-1", ParkItemCategory.Shop),
            });

        ParkRatingRankingResult ranking = Assert.Single(
            RatingRankingFactory.BuildParkRankings(sources, evidenceFacts: evidenceFacts));

        Assert.Equal(RankingEvidenceLevel.Provisional, ranking.Evidence?.Level);
        Assert.False(ranking.Evidence?.IsEligibleForMainRanking);
        Assert.Equal(RankingIneligibilityReason.TooFewUniqueContributors, ranking.Evidence?.IneligibilityReason);
        Assert.Equal(5, ranking.Evidence?.EligibleItemCount);
    }

    [Fact]
    public void BuildParkRankings_WhenCategoryFilterTargetsMultiCategoryPark_ShouldNotGrantMonoCategoryException()
    {
        List<RatingRankingItemResult> sources = new List<RatingRankingItemResult>
        {
            CreateSource(RatingTargetType.Park, "park-1", null, ratingCount: 3),
        };
        for (int index = 1; index <= 5; index += 1)
        {
            sources.Add(CreateSource(
                RatingTargetType.ParkItem,
                $"attraction-{index}",
                ParkItemCategory.Attraction,
                ratingCount: 10));
        }

        ParkRankingEvidenceFactsBatch evidenceFacts = new ParkRankingEvidenceFactsBatch(
            new[]
            {
                new ParkRankingContributorFacts(
                    "park-1",
                    UniqueContributorCount: 15,
                    RatingObservationCount: 53,
                    DirectParkContributorCount: 3,
                    ItemContributorCount: 12),
            },
            new[]
            {
                CreatePublicItem("attraction-1", ParkItemCategory.Attraction),
                CreatePublicItem("attraction-2", ParkItemCategory.Attraction),
                CreatePublicItem("attraction-3", ParkItemCategory.Attraction),
                CreatePublicItem("attraction-4", ParkItemCategory.Attraction),
                CreatePublicItem("attraction-5", ParkItemCategory.Attraction),
                CreatePublicItem("restaurant-1", ParkItemCategory.Restaurant),
            });

        ParkRatingRankingResult ranking = Assert.Single(RatingRankingFactory.BuildParkRankings(
            sources,
            ParkItemCategory.Attraction,
            evidenceFacts));

        Assert.Equal(RankingEvidenceLevel.Provisional, ranking.Evidence?.Level);
        Assert.Equal(3, ranking.UniqueContributorCount);
        Assert.Equal(3, ranking.RatingObservationCount);
        Assert.Equal(1, ranking.Evidence?.EligibleCategoryCount);
    }

    private static IReadOnlyCollection<RatingRankingItemResult> CreateSources()
    {
        return new[]
        {
            CreateSource(RatingTargetType.Park, "park-1", null, 10),
            CreateSource(RatingTargetType.ParkItem, "attraction-1", ParkItemCategory.Attraction, 10),
            CreateSource(RatingTargetType.ParkItem, "attraction-2", ParkItemCategory.Attraction, 10),
            CreateSource(RatingTargetType.ParkItem, "attraction-3", ParkItemCategory.Attraction, 10),
            CreateSource(RatingTargetType.ParkItem, "restaurant-1", ParkItemCategory.Restaurant, 10),
            CreateSource(RatingTargetType.ParkItem, "shop-1", ParkItemCategory.Shop, 10),
        };
    }

    private static RatingRankingItemResult CreateSource(
        RatingTargetType targetType,
        string targetId,
        ParkItemCategory? category,
        long ratingCount)
    {
        return new RatingRankingItemResult(
            targetType,
            targetId,
            targetType == RatingTargetType.Park ? "Demo Park" : targetId,
            "park-1",
            "Demo Park",
            category,
            category == ParkItemCategory.Attraction ? ParkItemType.RollerCoaster : null,
            ratingCount,
            ratingCount * 4.5,
            4.5,
            4.2)
        {
            UniqueContributorCount = ratingCount,
            AggregateIntegrityIsValid = true,
        };
    }

    private static PublicParkItemEvidenceFact CreatePublicItem(
        string targetId,
        ParkItemCategory category)
    {
        return new PublicParkItemEvidenceFact("park-1", targetId, category);
    }
}
