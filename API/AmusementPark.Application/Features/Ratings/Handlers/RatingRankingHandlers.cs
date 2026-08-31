using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetRatingRankingsQueryHandler : IQueryHandler<GetRatingRankingsQuery, ApplicationResult<PagedResult<ParkRatingRankingResult>>>
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRepository ratingRepository;
    private readonly IRatingEvidenceReader ratingEvidenceReader;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetRatingRankingsQueryHandler(
        IRatingRepository ratingRepository,
        IRatingEvidenceReader ratingEvidenceReader,
        PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.ratingEvidenceReader = ratingEvidenceReader;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<ParkRatingRankingResult>>> HandleAsync(GetRatingRankingsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkRatingRankingResult>>.Failure(errors);
        }

        RatingRankingSourceBatch sourceBatch = await this.ratingRepository.GetVisibleRankingSourcesAsync(
            query.ParkItemCategory,
            RankingSourceLimit,
            cancellationToken);
        IReadOnlyCollection<RatingRankingItemResult> sources = sourceBatch.Sources;
        IReadOnlyCollection<ParkRatingRankingResult> rankings = RatingRankingFactory.BuildParkRankings(
            sources,
            query.ParkItemCategory);
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

public sealed class GetParkItemRatingRankingsQueryHandler
    : IQueryHandler<GetParkItemRatingRankingsQuery, ApplicationResult<PagedResult<ParkItemRatingRankingResult>>>
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRepository ratingRepository;
    private readonly IRatingEvidenceReader ratingEvidenceReader;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetParkItemRatingRankingsQueryHandler(
        IRatingRepository ratingRepository,
        IRatingEvidenceReader ratingEvidenceReader,
        PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.ratingEvidenceReader = ratingEvidenceReader;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<ParkItemRatingRankingResult>>> HandleAsync(
        GetParkItemRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkItemRatingRankingResult>>.Failure(errors);
        }

        if (!Enum.IsDefined(query.ParkItemCategory))
        {
            return ApplicationResult<PagedResult<ParkItemRatingRankingResult>>.Failure(
                RatingApplicationErrors.InvalidParkItemCategory());
        }

        IReadOnlyCollection<RatingRankingItemResult> sources = await this.ratingRepository.GetVisibleParkItemRankingSourcesAsync(
            query.ParkItemCategory,
            RankingSourceLimit,
            cancellationToken);
        IReadOnlyCollection<ParkItemRatingRankingResult> rankings = RatingRankingFactory.BuildParkItemRankings(
            sources,
            query.ParkItemType);
        IReadOnlyCollection<ParkItemRatingRankingResult> filteredRankings = string.IsNullOrWhiteSpace(query.Search)
            ? rankings
            : rankings.Where(ranking =>
                    ranking.TargetName.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase)
                    || ranking.ParkName.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        PagedResult<ParkItemRatingRankingResult> result = RatingRankingPaging.BuildPage(
            filteredRankings,
            query.Paging.Page,
            query.Paging.PageSize);
        if (result.Items.Count > 0)
        {
            HashSet<string> resultTargetIds = result.Items
                .Select(static ranking => ranking.TargetId)
                .ToHashSet(StringComparer.Ordinal);
            IReadOnlyCollection<RatingRankingItemResult> resultSources = sources
                .Where(source => resultTargetIds.Contains(source.TargetId))
                .ToList();
            IReadOnlyCollection<RatingAggregateSourceFact> sourceFacts =
                await this.ratingEvidenceReader.ReadAggregateSourceFactsAsync(
                    resultSources.Select(static source => new RatingAggregateSourceTarget(
                            source.TargetType,
                            source.TargetId))
                        .Distinct()
                        .ToList(),
                    cancellationToken);
            IReadOnlyCollection<ParkItemRatingRankingResult> enrichedItems =
                RatingRankingFactory.ApplyParkItemEvidence(result.Items, resultSources, sourceFacts);
            result = new PagedResult<ParkItemRatingRankingResult>(
                enrichedItems,
                result.Page,
                result.PageSize,
                result.TotalItems);
        }

        return ApplicationResult<PagedResult<ParkItemRatingRankingResult>>.Success(result);
    }
}

public sealed class GetUserParkRatingRankingsQueryHandler
    : IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>>
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRepository ratingRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetUserParkRatingRankingsQueryHandler(
        IRatingRepository ratingRepository,
        PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<UserParkRatingRankingResult>>> HandleAsync(
        GetUserParkRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<PagedResult<UserParkRatingRankingResult>>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<UserParkRatingRankingResult>>.Failure(errors);
        }

        IReadOnlyCollection<UserRatingListItemResult> sources = query.PublicTargetsOnly
            ? await this.ratingRepository.GetVisibleUserRankingSourcesAsync(
                query.UserId.Trim(),
                RankingSourceLimit,
                cancellationToken)
            : await this.ratingRepository.GetUserRankingSourcesAsync(
                query.UserId.Trim(),
                RankingSourceLimit,
                cancellationToken);
        IReadOnlyCollection<UserParkRatingRankingResult> rankings = RatingRankingFactory.BuildUserParkRankings(sources);
        PagedResult<UserParkRatingRankingResult> result = string.IsNullOrWhiteSpace(query.ParkSearch)
            ? RatingRankingPaging.BuildPage(rankings, query.Paging.Page, query.Paging.PageSize)
            : BuildSearchWindow(rankings, query.ParkSearch.Trim(), query.Paging.PageSize);

        return ApplicationResult<PagedResult<UserParkRatingRankingResult>>.Success(result);
    }

    private static PagedResult<UserParkRatingRankingResult> BuildSearchWindow(
        IReadOnlyCollection<UserParkRatingRankingResult> rankings,
        string parkSearch,
        int requestedPageSize)
    {
        List<UserParkRatingRankingResult> orderedRankings = rankings.ToList();
        int matchIndex = orderedRankings.FindIndex(ranking =>
            ranking.ParkName.Contains(parkSearch, StringComparison.OrdinalIgnoreCase));
        if (matchIndex < 0)
        {
            return new PagedResult<UserParkRatingRankingResult>(
                Array.Empty<UserParkRatingRankingResult>(),
                1,
                requestedPageSize,
                0);
        }

        const int contextSize = 5;
        int startIndex = Math.Max(0, matchIndex - contextSize);
        int endIndex = Math.Min(orderedRankings.Count - 1, matchIndex + contextSize);
        List<UserParkRatingRankingResult> items = orderedRankings
            .Skip(startIndex)
            .Take(endIndex - startIndex + 1)
            .ToList();

        return new PagedResult<UserParkRatingRankingResult>(
            items,
            1,
            Math.Max(items.Count, 1),
            items.Count);
    }
}

public sealed class GetUserParkItemRatingRankingsQueryHandler
    : IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>
{
    private const int RankingSourceLimit = 5000;

    private readonly IRatingRepository ratingRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetUserParkItemRatingRankingsQueryHandler(
        IRatingRepository ratingRepository,
        PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> HandleAsync(
        GetUserParkItemRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(errors);
        }

        if (!Enum.IsDefined(query.ParkItemCategory))
        {
            return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(
                RatingApplicationErrors.InvalidParkItemCategory());
        }

        IReadOnlyCollection<UserRatingListItemResult> sources = query.PublicTargetsOnly
            ? await this.ratingRepository.GetVisibleUserRankingSourcesAsync(
                query.UserId.Trim(),
                RankingSourceLimit,
                cancellationToken)
            : await this.ratingRepository.GetUserRankingSourcesAsync(
                query.UserId.Trim(),
                RankingSourceLimit,
                cancellationToken);
        IReadOnlyCollection<UserParkItemRatingRankingResult> rankings = RatingRankingFactory.BuildUserParkItemRankings(
            sources,
            query.ParkItemCategory,
            query.ParkItemType);
        IReadOnlyCollection<UserParkItemRatingRankingResult> filteredRankings = string.IsNullOrWhiteSpace(query.Search)
            ? rankings
            : rankings.Where(ranking =>
                    ranking.Rating.TargetName.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase)
                    || (ranking.Rating.ParkName?.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        PagedResult<UserParkItemRatingRankingResult> result = RatingRankingPaging.BuildPage(
            filteredRankings,
            query.Paging.Page,
            query.Paging.PageSize);

        return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Success(result);
    }
}
