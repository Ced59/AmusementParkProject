using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetRatingRankingsQueryHandler : IQueryHandler<GetRatingRankingsQuery, ApplicationResult<PagedResult<ParkRatingRankingResult>>>
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRepository ratingRepository;
    private readonly IRatingEvidenceReader ratingEvidenceReader;
    private readonly PagedQueryValidator pagedQueryValidator;
    private readonly ICanonicalParkRatingRankingReader canonicalRankingReader;

    public GetRatingRankingsQueryHandler(
        IRatingRepository ratingRepository,
        IRatingEvidenceReader ratingEvidenceReader,
        PagedQueryValidator pagedQueryValidator,
        ICanonicalParkRatingRankingReader canonicalRankingReader)
    {
        this.ratingRepository = ratingRepository;
        this.ratingEvidenceReader = ratingEvidenceReader;
        this.pagedQueryValidator = pagedQueryValidator;
        this.canonicalRankingReader = canonicalRankingReader;
    }

    public async Task<ApplicationResult<PagedResult<ParkRatingRankingResult>>> HandleAsync(GetRatingRankingsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkRatingRankingResult>>.Failure(errors);
        }

        if (!query.ParkItemCategory.HasValue)
        {
            PagedResult<ParkRatingRankingResult> canonicalResult =
                await this.canonicalRankingReader.ReadAsync(
                    query.Paging.Page,
                    query.Paging.PageSize,
                    query.ParkSearch,
                    cancellationToken);
            return ApplicationResult<PagedResult<ParkRatingRankingResult>>.Success(canonicalResult);
        }

        RatingRankingSourceBatch sourceBatch = await this.ratingRepository.GetVisibleRankingSourcesAsync(
            query.ParkItemCategory,
            RankingSourceLimit,
            cancellationToken);
        IReadOnlyCollection<RatingRankingItemResult> sources = sourceBatch.Sources;
        IReadOnlyCollection<ParkRatingRankingResult> rankings = RatingRankingFactory.BuildParkTrends(
            sources,
            query.ParkItemCategory.Value);
        PagedResult<ParkRatingRankingResult> result = string.IsNullOrWhiteSpace(query.ParkSearch)
            ? RatingRankingPaging.BuildPage(rankings, query.Paging.Page, query.Paging.PageSize)
            : BuildSearchWindow(rankings, query.ParkSearch.Trim(), query.Paging.PageSize);
        if (result.Items.Count > 0 && !sourceBatch.IsTruncated)
        {
            HashSet<string> resultParkIds = result.Items
                .Select(static ranking => ranking.ParkId)
                .ToHashSet(StringComparer.Ordinal);
            IReadOnlyCollection<RatingRankingItemResult> resultSources = sources
                .Where(source => resultParkIds.Contains(source.ParkId))
                .ToList();
            ParkRankingEvidenceFactsBatch evidenceFacts = await this.ratingEvidenceReader.ReadParkRankingFactsAsync(
                resultSources.Select(static source => new RatingEvidenceTarget(
                        source.TargetType,
                        source.TargetId,
                        source.ParkId))
                    .Distinct()
                    .ToList(),
                cancellationToken);
            IReadOnlyCollection<ParkRatingRankingResult> enrichedItems = RatingRankingFactory.ApplyParkEvidence(
                result.Items,
                resultSources,
                evidenceFacts,
                query.ParkItemCategory);
            result = new PagedResult<ParkRatingRankingResult>(
                enrichedItems,
                result.Page,
                result.PageSize,
                result.TotalItems);
        }

        return ApplicationResult<PagedResult<ParkRatingRankingResult>>.Success(result);
    }

    private static PagedResult<ParkRatingRankingResult> BuildSearchWindow(IReadOnlyCollection<ParkRatingRankingResult> rankings, string parkSearch, int requestedPageSize)
    {
        List<ParkRatingRankingResult> orderedRankings = rankings.ToList();
        int matchIndex = orderedRankings.FindIndex(ranking => ranking.ParkName.Contains(parkSearch, StringComparison.OrdinalIgnoreCase));
        if (matchIndex < 0)
        {
            return new PagedResult<ParkRatingRankingResult>(Array.Empty<ParkRatingRankingResult>(), 1, requestedPageSize, 0);
        }

        const int contextSize = 5;
        int startIndex = Math.Max(0, matchIndex - contextSize);
        int endIndex = Math.Min(orderedRankings.Count - 1, matchIndex + contextSize);
        List<ParkRatingRankingResult> items = orderedRankings
            .Skip(startIndex)
            .Take(endIndex - startIndex + 1)
            .ToList();

        return new PagedResult<ParkRatingRankingResult>(items, 1, Math.Max(items.Count, 1), items.Count);
    }
}
