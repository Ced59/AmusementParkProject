using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetPassportProfileShareSelectionQueryHandler
    : IQueryHandler<
        GetPassportProfileShareSelectionQuery,
        ApplicationResult<PassportProfileShareSelectionResult>>
{
    private readonly IPassportProfileSourceReader sourceReader;
    private readonly IParkRepository parkRepository;
    private readonly IRatingRepository ratingRepository;
    private readonly ISharePublicationRepository publicationRepository;
    private readonly IPassportProfileShareSnapshotRepository snapshotRepository;

    public GetPassportProfileShareSelectionQueryHandler(
        IPassportProfileSourceReader sourceReader,
        IParkRepository parkRepository,
        IRatingRepository ratingRepository,
        ISharePublicationRepository publicationRepository,
        IPassportProfileShareSnapshotRepository snapshotRepository)
    {
        this.sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
        this.parkRepository = parkRepository ?? throw new ArgumentNullException(nameof(parkRepository));
        this.ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.snapshotRepository = snapshotRepository
            ?? throw new ArgumentNullException(nameof(snapshotRepository));
    }

    public async Task<ApplicationResult<PassportProfileShareSelectionResult>> HandleAsync(
        GetPassportProfileShareSelectionQuery query,
        CancellationToken cancellationToken = default)
    {
        string ownerUserId = query.OwnerUserId?.Trim() ?? string.Empty;
        if (ownerUserId.Length == 0)
        {
            return ApplicationResult<PassportProfileShareSelectionResult>.Failure(
                SharingApplicationErrors.InvalidSource());
        }

        PassportProfileSourceData source = await this.sourceReader.ReadOwnedCompletedPassportAsync(
            ownerUserId,
            cancellationToken);
        string[] parkIds = source.Visits
            .Select(static visit => visit.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<Park> parks = parkIds.Length == 0
            ? Array.Empty<Park>()
            : await this.parkRepository.GetByIdsAsync(parkIds, cancellationToken);
        IReadOnlyDictionary<string, Park> publicParks = parks
            .Where(static park => park.IsVisible
                && !string.IsNullOrWhiteSpace(park.Id)
                && !string.IsNullOrWhiteSpace(park.Name))
            .GroupBy(static park => park.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First(),
                StringComparer.Ordinal);
        PassportProfileShareYearCandidateResult[] years = source.Visits
            .Where(visit => publicParks.ContainsKey(visit.ParkId))
            .GroupBy(static visit => visit.VisitDate.Year)
            .OrderByDescending(static group => group.Key)
            .Select(static group => new PassportProfileShareYearCandidateResult(
                group.Key,
                group.LongCount()))
            .ToArray();
        PassportProfileShareParkCandidateResult[] parkResults = source.Visits
            .Where(visit => publicParks.ContainsKey(visit.ParkId))
            .GroupBy(static visit => visit.ParkId, StringComparer.Ordinal)
            .Select(group => new PassportProfileShareParkCandidateResult(
                group.Key,
                publicParks[group.Key].Name!.Trim(),
                NormalizeCountryCode(publicParks[group.Key].CountryCode),
                group.LongCount()))
            .OrderByDescending(static park => park.VisitCount)
            .ThenBy(static park => park.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        IReadOnlyCollection<UserRatingListItemResult> ratingSources =
            await this.ratingRepository.GetVisibleUserRankingSourcesAsync(
                ownerUserId,
                PassportProfileShareInputNormalizer.MaximumSelectedRatings,
                cancellationToken);
        PassportProfileShareRatingCandidateResult[] ratings = ratingSources
            .Where(rating => publicParks.ContainsKey(rating.ParkId))
            .Select(static rating => new PassportProfileShareRatingCandidateResult(
                PassportProfileRatingSelectionKey.Create(rating.TargetType, rating.TargetId),
                rating.TargetName.Trim(),
                NormalizeOptional(rating.ParkName),
                rating.Value))
            .OrderByDescending(static rating => rating.Rating)
            .ThenBy(static rating => rating.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string sourceScopeKey = PassportProfileShareSourceScope.Create(ownerUserId);
        SharePublication? publication = await this.publicationRepository.GetOwnedBySourceAsync(
            ownerUserId,
            SharePublicationType.PassportProfile,
            sourceScopeKey,
            cancellationToken);
        PassportProfileShareSnapshot? snapshot = publication is null
            || publication.PublicationVersion < 1
            ? null
            : await this.snapshotRepository.GetAsync(
                publication.Id,
                publication.PublicationVersion,
                cancellationToken);
        if (publication?.IsResolvable == true && snapshot is null)
        {
            return ApplicationResult<PassportProfileShareSelectionResult>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        PassportProfileShareInput? saved = snapshot?.Selection;
        return ApplicationResult<PassportProfileShareSelectionResult>.Success(
            new PassportProfileShareSelectionResult(
                years,
                parkResults,
                ratings,
                saved?.SelectedYears,
                saved?.SelectedParkIds,
                saved?.SelectedRatingKeys,
                saved?.PublicCaption,
                saved?.Visibility ?? ShareVisibility.Unlisted,
                saved?.AllowsComparisons ?? false,
                snapshot is not null));
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
