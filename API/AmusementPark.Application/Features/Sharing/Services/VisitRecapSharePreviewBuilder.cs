using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class VisitRecapSharePreviewBuilder : IVisitRecapSharePreviewBuilder
{
    private readonly IVisitRecapSourceReader sourceReader;
    private readonly IVisitRecapShareSourceVersionProvider sourceVersionProvider;
    private readonly IVisitRecapPublicParkReader publicParkReader;
    private readonly IVisitTargetResolver targetResolver;

    public VisitRecapSharePreviewBuilder(
        IVisitRecapSourceReader sourceReader,
        IVisitRecapShareSourceVersionProvider sourceVersionProvider,
        IVisitRecapPublicParkReader publicParkReader,
        IVisitTargetResolver targetResolver)
    {
        this.sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
        this.sourceVersionProvider = sourceVersionProvider
            ?? throw new ArgumentNullException(nameof(sourceVersionProvider));
        this.publicParkReader = publicParkReader
            ?? throw new ArgumentNullException(nameof(publicParkReader));
        this.targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
    }

    public SharePublicationType PublicationType => SharePublicationType.VisitRecap;

    public async Task<ApplicationResult<VisitRecapShareCandidatesResult>> GetCandidatesAsync(
        string ownerUserId,
        string sourceId,
        bool includeMissedItems,
        IReadOnlyCollection<string>? preferredParkItemIds,
        CancellationToken cancellationToken)
    {
        string normalizedOwner = ownerUserId?.Trim() ?? string.Empty;
        string normalizedSource = sourceId?.Trim() ?? string.Empty;
        VisitRecapSourceData? source = await this.sourceReader.GetOwnedCompletedAsync(
            normalizedOwner,
            normalizedSource,
            cancellationToken);
        if (source is null)
        {
            return ApplicationResult<VisitRecapShareCandidatesResult>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        if (!source.IsComplete)
        {
            return ApplicationResult<VisitRecapShareCandidatesResult>.Failure(
                SharingApplicationErrors.VisitRecapTooLarge());
        }

        if (!source.Revision.IsStable)
        {
            return ApplicationResult<VisitRecapShareCandidatesResult>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        string? parkName = await this.publicParkReader.GetVisibleNameAsync(
            source.ParkId,
            cancellationToken);
        if (parkName is null)
        {
            return ApplicationResult<VisitRecapShareCandidatesResult>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        string[] parkItemIds = source.Occurrences
            .Select(static occurrence => occurrence.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, VisitTarget> targets = parkItemIds.Length == 0
            ? new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            : await this.targetResolver.ResolveAsync(parkItemIds, cancellationToken);
        ShareContentPolicy candidatePolicy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Hidden,
            includeMissedItems
                ? new[] { ShareContentField.MissedItems }
                : Array.Empty<ShareContentField>());
        IReadOnlySet<string> eligibleIds = ResolveEligibleIds(source, candidatePolicy, targets);
        List<VisitRecapShareItemResult> candidates = BuildItems(
            source,
            targets,
            eligibleIds,
            includesRideCounts: false,
            includesRatings: false,
            out bool _);
        int totalCount = candidates.Count;
        HashSet<string> preferredIds = (preferredParkItemIds ?? Array.Empty<string>())
            .Select(static value => value?.Trim() ?? string.Empty)
            .Where(static value => value.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        VisitRecapShareItemResult[] boundedCandidates = candidates
            .OrderByDescending(candidate => preferredIds.Contains(candidate.ParkItemId))
            .Take(VisitRecapShareInputNormalizer.MaximumSelectedItemCount)
            .ToArray();
        return ApplicationResult<VisitRecapShareCandidatesResult>.Success(
            new VisitRecapShareCandidatesResult(
                boundedCandidates,
                totalCount,
                totalCount > boundedCandidates.Length));
    }

    public Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        string? sourceId,
        ShareContentPolicy contentPolicy,
        CancellationToken cancellationToken)
    {
        string normalizedSourceId = sourceId?.Trim() ?? string.Empty;
        return this.BuildAsync(
            ownerUserId,
            normalizedSourceId,
            contentPolicy,
            null,
            cancellationToken);
    }

    public async Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        string sourceId,
        ShareContentPolicy contentPolicy,
        VisitRecapShareInput? input,
        CancellationToken cancellationToken)
    {
        ApplicationResult<VisitRecapShareInput> normalizedInputResult =
            VisitRecapShareInputNormalizer.Normalize(input, contentPolicy);
        if (!normalizedInputResult.IsSuccess || normalizedInputResult.Value is null)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                normalizedInputResult.Errors);
        }

        string normalizedOwner = ownerUserId?.Trim() ?? string.Empty;
        string normalizedSource = sourceId?.Trim() ?? string.Empty;
        ApplicationResult<long> versionBefore =
            await this.sourceVersionProvider.GetOwnedSourceVersionAsync(
                normalizedOwner,
                normalizedSource,
                cancellationToken);
        if (!versionBefore.IsSuccess)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(versionBefore.Errors);
        }

        VisitRecapSourceData? source = await this.sourceReader.GetOwnedCompletedAsync(
            normalizedOwner,
            normalizedSource,
            cancellationToken);
        if (source is null)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        if (!source.IsComplete)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.VisitRecapTooLarge());
        }

        if (!source.Revision.IsStable
            || !CanExposeDate(source.Date, contentPolicy.DatePrecision))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                source.Revision.IsStable
                    ? SharingApplicationErrors.InvalidVisitRecapSelection()
                    : SharingApplicationErrors.SourceChangedDuringPreview());
        }

        string[] parkItemIds = source.Occurrences
            .Select(static occurrence => occurrence.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, VisitTarget> targets = parkItemIds.Length == 0
            ? new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            : await this.targetResolver.ResolveAsync(parkItemIds, cancellationToken);
        string? parkName = await this.publicParkReader.GetVisibleNameAsync(
            source.ParkId,
            cancellationToken);
        if (parkName is null)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        ApplicationResult<long> versionAfter =
            await this.sourceVersionProvider.GetOwnedSourceVersionAsync(
                normalizedOwner,
                normalizedSource,
                cancellationToken);
        if (!versionAfter.IsSuccess
            || versionAfter.Value != versionBefore.Value)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        VisitRecapShareInput normalizedInput = normalizedInputResult.Value;
        IReadOnlySet<string> eligibleIds = ResolveEligibleIds(
            source,
            contentPolicy,
            targets);
        if (normalizedInput.SelectedParkItemIds is not null
            && normalizedInput.SelectedParkItemIds.Any(id => !eligibleIds.Contains(id)))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.InvalidVisitRecapSelection());
        }

        if (normalizedInput.SelectedParkItemIds is null
            && eligibleIds.Count > VisitRecapShareInputNormalizer.MaximumSelectedItemCount)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.VisitRecapTooLarge());
        }

        IReadOnlySet<string> selectedIds = normalizedInput.SelectedParkItemIds is null
            ? eligibleIds
            : normalizedInput.SelectedParkItemIds.ToHashSet(StringComparer.Ordinal);
        bool includesRideCounts = contentPolicy.Includes(ShareContentField.RideCount);
        bool includesRatings = contentPolicy.Includes(ShareContentField.TemporalRatings);
        List<VisitRecapShareItemResult> items = BuildItems(
            source,
            targets,
            selectedIds,
            includesRideCounts,
            includesRatings,
            out bool hasIncompleteItems);
        hasIncompleteItems = hasIncompleteItems
            || source.Occurrences.Any(occurrence =>
                (occurrence.Status == RideOccurrenceStatus.Completed
                    || contentPolicy.Includes(ShareContentField.MissedItems))
                && !CanExposeTarget(source.ParkId, occurrence, targets));
        VisitRecapShareHighlightResult? topRatedItem = includesRatings
            ? items
                .Where(static item => item.AverageRating.HasValue)
                .OrderByDescending(static item => item.AverageRating)
                .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static item => new VisitRecapShareHighlightResult(
                    item.Name,
                    null,
                    item.AverageRating))
                .FirstOrDefault()
            : null;
        VisitRecapShareHighlightResult? mostRepeatedItem = includesRideCounts
            ? items
                .Where(static item => item.RideCount > 0)
                .OrderByDescending(static item => item.RideCount)
                .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static item => new VisitRecapShareHighlightResult(
                    item.Name,
                    item.RideCount,
                    null))
                .FirstOrDefault()
            : null;
        VisitRecapSourceOccurrence[] completedOccurrences = source.Occurrences
            .Where(occurrence => occurrence.Status == RideOccurrenceStatus.Completed
                && eligibleIds.Contains(occurrence.ParkItemId))
            .ToArray();
        string[] categories = items
            .Select(static item => item.Category)
            .Where(static category => category is not null)
            .Select(static category => category!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static category => category, StringComparer.Ordinal)
            .ToArray();
        VisitRecapSharePreviewResult visitRecap = new VisitRecapSharePreviewResult(
            source.ParkId,
            parkName,
            BuildPublicDate(source.Date, contentPolicy.DatePrecision),
            includesRideCounts
                ? completedOccurrences.Select(static occurrence => occurrence.ParkItemId)
                    .Distinct(StringComparer.Ordinal).Count()
                : null,
            includesRideCounts ? completedOccurrences.Length : null,
            categories,
            includesRatings ? source.ParkRating?.DoubleValue : null,
            topRatedItem,
            mostRepeatedItem,
            items,
            contentPolicy.Includes(ShareContentField.PublicCaption)
                ? normalizedInput.PublicCaption
                : null,
            contentPolicy.DatePrecision == ShareDatePrecision.Hidden
                || (int)contentPolicy.DatePrecision < (int)source.Date.Precision,
            includesRatings
                && completedOccurrences.Any(static occurrence => !occurrence.Rating.HasValue),
            hasIncompleteItems);
        string fingerprint = VisitRecapShareInputNormalizer.CreateFingerprint(normalizedInput);
        SharePublicationPreviewResult preview = new SharePublicationPreviewResult(
            this.PublicationType,
            versionAfter.Value,
            contentPolicy.SchemaVersion,
            contentPolicy.DatePrecision,
            contentPolicy.IncludedFields,
            null,
            VisitRecap: visitRecap,
            ContentFingerprint: fingerprint);
        return ApplicationResult<SharePublicationPreviewResult>.Success(preview);
    }

    private static IReadOnlySet<string> ResolveEligibleIds(
        VisitRecapSourceData source,
        ShareContentPolicy contentPolicy,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        bool includesMissedItems = contentPolicy.Includes(ShareContentField.MissedItems);
        return source.Occurrences
            .Where(occurrence => occurrence.Status == RideOccurrenceStatus.Completed
                || includesMissedItems)
            .Where(occurrence => CanExposeTarget(source.ParkId, occurrence, targets))
            .Select(static occurrence => occurrence.ParkItemId)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static List<VisitRecapShareItemResult> BuildItems(
        VisitRecapSourceData source,
        IReadOnlyDictionary<string, VisitTarget> targets,
        IReadOnlySet<string> selectedIds,
        bool includesRideCounts,
        bool includesRatings,
        out bool hasIncompleteItems)
    {
        hasIncompleteItems = false;
        List<VisitRecapShareItemResult> items = new List<VisitRecapShareItemResult>();
        foreach (IGrouping<string, VisitRecapSourceOccurrence> group in source.Occurrences
                     .Where(occurrence => selectedIds.Contains(occurrence.ParkItemId))
                     .GroupBy(static occurrence => occurrence.ParkItemId, StringComparer.Ordinal))
        {
            VisitRecapSourceOccurrence first = group.First();
            targets.TryGetValue(group.Key, out VisitTarget? target);
            if (!CanExposeTarget(source.ParkId, first, targets))
            {
                hasIncompleteItems = true;
                continue;
            }

            string? name = NormalizeOptional(target?.Name) ?? NormalizeOptional(first.HistoricalName);
            if (name is null)
            {
                hasIncompleteItems = true;
                continue;
            }

            ParkItemCategory? category = target?.Category ?? first.HistoricalCategory;
            VisitRecapSourceOccurrence[] completed = group
                .Where(static occurrence => occurrence.Status == RideOccurrenceStatus.Completed)
                .ToArray();
            double? averageRating = includesRatings && completed.Any(static value => value.Rating.HasValue)
                ? completed.Where(static value => value.Rating.HasValue)
                    .Average(static value => value.Rating!.Value.DoubleValue)
                : null;
            items.Add(new VisitRecapShareItemResult(
                group.Key,
                name,
                category?.ToString(),
                includesRideCounts ? completed.Length : null,
                averageRating,
                completed.Length == 0));
        }

        return items
            .OrderByDescending(static item => item.RideCount ?? 0)
            .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool CanExposeTarget(
        string sourceParkId,
        VisitRecapSourceOccurrence occurrence,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        if (!targets.TryGetValue(occurrence.ParkItemId, out VisitTarget? target))
        {
            return NormalizeOptional(occurrence.HistoricalName) is not null;
        }

        return target.IsVisible
            && string.Equals(target.ParkId, sourceParkId, StringComparison.Ordinal);
    }

    private static bool CanExposeDate(VisitDate sourceDate, ShareDatePrecision precision)
    {
        return precision == ShareDatePrecision.Hidden
            || (int)precision <= (int)sourceDate.Precision;
    }

    private static VisitRecapShareDateResult? BuildPublicDate(
        VisitDate sourceDate,
        ShareDatePrecision precision)
    {
        return precision == ShareDatePrecision.Hidden
            ? null
            : new VisitRecapShareDateResult(
                sourceDate.Year,
                precision >= ShareDatePrecision.Month ? sourceDate.Month : null,
                precision == ShareDatePrecision.Day ? sourceDate.Day : null,
                precision,
                sourceDate.IsApproximate);
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }
}
