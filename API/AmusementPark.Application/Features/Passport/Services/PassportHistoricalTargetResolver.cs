using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class PassportHistoricalTargetResolver : IPassportHistoricalTargetResolver
{
    private const int TargetResolutionBatchSize = 200;

    private readonly PublicParkHistoricalDataLoader historicalDataLoader;
    private readonly IParkHistoricalSnapshotBuilder snapshotBuilder;
    private readonly IVisitTargetResolver currentTargetResolver;
    private readonly IImageRepository imageRepository;

    public PassportHistoricalTargetResolver(
        PublicParkHistoricalDataLoader historicalDataLoader,
        IParkHistoricalSnapshotBuilder snapshotBuilder,
        IVisitTargetResolver currentTargetResolver,
        IImageRepository imageRepository)
    {
        this.historicalDataLoader = historicalDataLoader;
        this.snapshotBuilder = snapshotBuilder;
        this.currentTargetResolver = currentTargetResolver;
        this.imageRepository = imageRepository;
    }

    public async Task<PassportHistoricalTargetContext> ResolveAllAsync(
        Visit visit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return await this.ResolveCoreAsync(
            visit.ParkId,
            visit.Date,
            null,
            false,
            cancellationToken);
    }

    public async Task<PassportHistoricalTargetContext> ResolveAsync(
        Visit visit,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        ArgumentNullException.ThrowIfNull(parkItemIds);
        string[] normalizedIds = parkItemIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return EmptyContext();
        }

        return await this.ResolveCoreAsync(
            visit.ParkId,
            visit.Date,
            normalizedIds,
            false,
            cancellationToken);
    }

    public async Task<PassportHistoricalTargetContext> ResolveRecordedAsync(
        Visit visit,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        ArgumentNullException.ThrowIfNull(parkItemIds);
        string[] normalizedIds = parkItemIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return EmptyContext();
        }

        return await this.ResolveCoreAsync(
            visit.ParkId,
            visit.Date,
            normalizedIds,
            true,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> ResolveMainImageIdsAsync(
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkItemIds);
        string[] normalizedIds = parkItemIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        IReadOnlyDictionary<string, VisitTarget> currentTargets =
            await this.ResolveCurrentTargetsAsync(normalizedIds, cancellationToken);
        return await this.ResolveImageIdsAsync(currentTargets.Values, cancellationToken);
    }

    public async Task<PassportHistoricalTargetContext> ResolveAsync(
        string parkId,
        VisitDate visitDate,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        ArgumentNullException.ThrowIfNull(visitDate);
        ArgumentNullException.ThrowIfNull(parkItemIds);
        string[] normalizedIds = parkItemIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedParkId.Length == 0 || normalizedIds.Length == 0)
        {
            return EmptyContext();
        }

        return await this.ResolveCoreAsync(
            normalizedParkId,
            visitDate,
            normalizedIds,
            false,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<VisitDate, PassportHistoricalTargetContext>> ResolveManyAsync(
        string parkId,
        IReadOnlyCollection<VisitDate> visitDates,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        ArgumentNullException.ThrowIfNull(visitDates);
        ArgumentNullException.ThrowIfNull(parkItemIds);
        VisitDate[] normalizedDates = visitDates.Distinct().ToArray();
        string[] normalizedIds = parkItemIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedParkId.Length == 0
            || normalizedDates.Length == 0
            || normalizedIds.Length == 0)
        {
            return new Dictionary<VisitDate, PassportHistoricalTargetContext>();
        }

        PublicParkHistoricalData? data = await this.historicalDataLoader.LoadAsync(
            normalizedParkId,
            cancellationToken);
        IReadOnlyDictionary<string, VisitTarget> currentTargets =
            await this.ResolveCurrentTargetsAsync(normalizedIds, cancellationToken);
        if (data is null)
        {
            PassportHistoricalTargetContext fallback = CreateCurrentFallbackContext(
                normalizedParkId,
                currentTargets.Values,
                false);
            return normalizedDates.ToDictionary(
                static date => date,
                _ => fallback);
        }

        return normalizedDates.ToDictionary(
            static date => date,
            date => this.BuildContext(
                normalizedParkId,
                date,
                normalizedIds,
                false,
                data,
                currentTargets));
    }

    private async Task<PassportHistoricalTargetContext> ResolveCoreAsync(
        string parkId,
        VisitDate visitDate,
        IReadOnlyCollection<string>? requestedIds,
        bool includeHiddenCurrentFallback,
        CancellationToken cancellationToken)
    {
        PublicParkHistoricalData? data = await this.historicalDataLoader.LoadAsync(
            parkId,
            cancellationToken);
        if (data is null)
        {
            return requestedIds is null
                ? EmptyContext()
                : await this.ResolveCurrentFallbackAsync(
                    parkId,
                    requestedIds,
                    includeHiddenCurrentFallback,
                    cancellationToken);
        }

        string[] currentTargetIds = requestedIds is null
            ? data.Subjects
                .Where(static subject => subject.Type == HistoricalSubjectType.ParkItem)
                .Select(static subject => subject.Id)
                .Distinct(StringComparer.Ordinal)
                .ToArray()
            : requestedIds.Distinct(StringComparer.Ordinal).ToArray();
        IReadOnlyDictionary<string, VisitTarget> currentTargets =
            await this.ResolveCurrentTargetsAsync(currentTargetIds, cancellationToken);
        return this.BuildContext(
            parkId,
            visitDate,
            requestedIds,
            includeHiddenCurrentFallback,
            data,
            currentTargets);
    }

    private PassportHistoricalTargetContext BuildContext(
        string parkId,
        VisitDate visitDate,
        IReadOnlyCollection<string>? requestedIds,
        bool includeHiddenCurrentFallback,
        PublicParkHistoricalData data,
        IReadOnlyDictionary<string, VisitTarget> currentTargets)
    {
        ParkHistoricalSnapshot snapshot = this.snapshotBuilder.Build(
            parkId,
            ToHistoricalInstant(visitDate),
            data.Subjects,
            data.Facts);
        HashSet<string>? requested = requestedIds?.ToHashSet(StringComparer.Ordinal);
        HistoricalSubjectSnapshot[] subjectSnapshots = snapshot.Subjects
            .Where(subject => subject.Subject.Type == HistoricalSubjectType.ParkItem
                && (requested is null || requested.Contains(subject.Subject.Id)))
            .ToArray();
        Dictionary<string, PassportHistoricalTarget> targets = new(StringComparer.Ordinal);
        foreach (HistoricalSubjectSnapshot subject in subjectSnapshots)
        {
            currentTargets.TryGetValue(subject.Subject.Id, out VisitTarget? currentTarget);
            string? category = ResolveKnownAttribute(subject, HistoricalAttributeKind.Category)
                ?? currentTarget?.Category.ToString();
            if (!string.Equals(
                    category,
                    ParkItemCategory.Attraction.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string name = ResolveKnownAttribute(subject, HistoricalAttributeKind.Name)
                ?? subject.Subject.HistoricalLabel;
            string? zoneId = ResolveKnownAttribute(subject, HistoricalAttributeKind.Zone)
                ?? currentTarget?.ZoneId;
            HistoricalConsistency consistency =
                RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(subject.OperationalState);
            targets[subject.Subject.Id] = new PassportHistoricalTarget(
                subject.Subject.Id,
                parkId,
                name,
                ParkItemCategory.Attraction.ToString(),
                subject.OperationalState,
                consistency,
                CreateHistoricalTargetReference(name, ParkItemCategory.Attraction.ToString()),
                currentTarget is null || !currentTarget.IsVisible,
                null,
                zoneId,
                currentTarget?.LifecycleStatus,
                currentTarget?.OpeningDate,
                currentTarget?.ClosingDate);
        }

        if (requested is not null)
        {
            string[] missingIds = requested
                .Where(id => !targets.ContainsKey(id))
                .ToArray();
            if (missingIds.Length > 0)
            {
                PassportHistoricalTargetContext fallback = CreateCurrentFallbackContext(
                    parkId,
                    currentTargets.Values.Where(target => missingIds.Contains(
                        target.ParkItemId,
                        StringComparer.Ordinal)),
                    includeHiddenCurrentFallback);
                foreach ((string id, PassportHistoricalTarget target) in fallback.Targets)
                {
                    targets.TryAdd(id, target);
                }
            }
        }

        return new PassportHistoricalTargetContext(
            targets,
            snapshot.Coverage.Status,
            CalculateCoveragePercent(snapshot.Coverage),
            snapshot.MethodologyVersion);
    }

    private async Task<PassportHistoricalTargetContext> ResolveCurrentFallbackAsync(
        string parkId,
        IReadOnlyCollection<string> requestedIds,
        bool includeHiddenCurrentTargets,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, VisitTarget> currentTargets =
            await this.ResolveCurrentTargetsAsync(requestedIds, cancellationToken);
        return CreateCurrentFallbackContext(
            parkId,
            currentTargets.Values,
            includeHiddenCurrentTargets);
    }

    private static PassportHistoricalTargetContext CreateCurrentFallbackContext(
        string parkId,
        IEnumerable<VisitTarget> currentTargets,
        bool includeHiddenCurrentTargets)
    {
        Dictionary<string, PassportHistoricalTarget> targets = currentTargets
            .Where(target => (includeHiddenCurrentTargets || target.IsVisible)
                && string.Equals(target.ParkId, parkId, StringComparison.Ordinal)
                && target.Category == ParkItemCategory.Attraction)
            .ToDictionary(
                static target => target.ParkItemId,
                target => new PassportHistoricalTarget(
                    target.ParkItemId,
                    target.ParkId,
                    target.Name,
                    target.Category.ToString(),
                    HistoricalOperationalState.Unknown,
                    HistoricalConsistency.Unverified,
                    CreateHistoricalTargetReference(target.Name, target.Category.ToString()),
                    !target.IsVisible,
                    null,
                    target.ZoneId,
                    target.LifecycleStatus,
                    target.OpeningDate,
                    target.ClosingDate,
                    includeHiddenCurrentTargets),
                StringComparer.Ordinal);
        return new PassportHistoricalTargetContext(
            targets,
            HistoricalCoverageStatus.Partial,
            0,
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion);
    }

    private async Task<IReadOnlyDictionary<string, VisitTarget>> ResolveCurrentTargetsAsync(
        IReadOnlyCollection<string> targetIds,
        CancellationToken cancellationToken)
    {
        Dictionary<string, VisitTarget> targets = new(StringComparer.Ordinal);
        string[] ids = targetIds.Distinct(StringComparer.Ordinal).ToArray();
        for (int index = 0; index < ids.Length; index += TargetResolutionBatchSize)
        {
            IReadOnlyDictionary<string, VisitTarget> batch =
                await this.currentTargetResolver.ResolveAsync(
                    ids.Skip(index).Take(TargetResolutionBatchSize).ToArray(),
                    cancellationToken);
            foreach ((string id, VisitTarget target) in batch)
            {
                targets.TryAdd(id, target);
            }
        }

        return targets;
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveImageIdsAsync(
        IEnumerable<VisitTarget> currentTargets,
        CancellationToken cancellationToken)
    {
        string[] publicIds = currentTargets
            .Where(static target => target.IsVisible)
            .Select(static target => target.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return publicIds.Length == 0
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : await this.imageRepository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.ParkItem,
                publicIds,
                ImageCategory.ParkItem,
                true,
                cancellationToken);
    }

    private static HistoricalInstant ToHistoricalInstant(VisitDate visitDate)
    {
        return visitDate.Precision switch
        {
            VisitDatePrecision.Day => HistoricalInstant.ForDay(
                visitDate.Year,
                visitDate.Month!.Value,
                visitDate.Day!.Value),
            VisitDatePrecision.Month => HistoricalInstant.ForMonth(
                visitDate.Year,
                visitDate.Month!.Value),
            VisitDatePrecision.Year => HistoricalInstant.ForYear(visitDate.Year),
            _ => throw new ArgumentOutOfRangeException(nameof(visitDate)),
        };
    }

    private static HistoricalTargetReference CreateHistoricalTargetReference(
        string name,
        string? category)
    {
        string normalizedName = name.Trim();
        if (normalizedName.Length > HistoricalTargetReference.MaximumNameLength)
        {
            int snapshotLength = HistoricalTargetReference.MaximumNameLength;
            if (char.IsHighSurrogate(normalizedName[snapshotLength - 1])
                && char.IsLowSurrogate(normalizedName[snapshotLength]))
            {
                snapshotLength -= 1;
            }

            normalizedName = normalizedName[..snapshotLength].TrimEnd();
        }

        return new HistoricalTargetReference(normalizedName, category);
    }

    private static string? ResolveKnownAttribute(
        HistoricalSubjectSnapshot subject,
        HistoricalAttributeKind kind)
    {
        HistoricalAttributeSnapshot? attribute = subject.Attributes
            .SingleOrDefault(value => value.Kind == kind);
        return attribute?.State == HistoricalAttributeValueState.Known
            ? attribute.Value
            : null;
    }

    private static int CalculateCoveragePercent(HistoricalCoverage coverage)
    {
        if (coverage.TotalSubjectCount == 0)
        {
            return 0;
        }

        decimal covered = coverage.ReliablePeriodSubjectCount
            + (coverage.PartialPeriodSubjectCount * 0.5m);
        return decimal.ToInt32(decimal.Round(
            covered * 100m / coverage.TotalSubjectCount,
            0,
            MidpointRounding.AwayFromZero));
    }

    private static PassportHistoricalTargetContext EmptyContext()
    {
        return new PassportHistoricalTargetContext(
            new Dictionary<string, PassportHistoricalTarget>(StringComparer.Ordinal),
            HistoricalCoverageStatus.Partial,
            0,
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion);
    }
}
