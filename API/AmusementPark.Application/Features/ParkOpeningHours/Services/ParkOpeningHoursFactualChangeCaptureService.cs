using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Models;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursFactualChangeCaptureService
    : IParkOpeningHoursFactualChangeCapture
{
    private readonly IFactualChangeCaptureService factualChangeCaptureService;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly ILogger<ParkOpeningHoursFactualChangeCaptureService> logger;

    public ParkOpeningHoursFactualChangeCaptureService(
        IFactualChangeCaptureService factualChangeCaptureService,
        IParkOpeningHoursRepository openingHoursRepository,
        ILogger<ParkOpeningHoursFactualChangeCaptureService> logger)
    {
        this.factualChangeCaptureService = factualChangeCaptureService;
        this.openingHoursRepository = openingHoursRepository;
        this.logger = logger;
    }

    public ParkOpeningHoursFactualChangeDraft? Prepare(
        Park park,
        ParkOpeningHoursSchedule? previousSchedule,
        ParkOpeningHoursSchedule currentSchedule)
    {
        ArgumentNullException.ThrowIfNull(park);
        ArgumentNullException.ThrowIfNull(currentSchedule);
        if (!CanCapture(park, currentSchedule))
        {
            return null;
        }

        FactValue? previousValue = OpeningCalendarFactSnapshot.Create(previousSchedule);
        FactValue? newValue = OpeningCalendarFactSnapshot.Create(currentSchedule);
        string parkName = park.Name!.Trim();
        DateTime verifiedAtUtc = currentSchedule.LastVerifiedAtUtc!.Value;
        SourceReference source = new SourceReference(
            SourceReferenceType.OfficialWebsite,
            parkName,
            $"Calendrier officiel — {parkName}",
            currentSchedule.SourceUrl!,
            verifiedAtUtc);
        return new ParkOpeningHoursFactualChangeDraft(
            previousValue is null && newValue is not null
                ? FactualEventType.OpeningCalendarPublished
                : FactualEventType.OpeningCalendarChanged,
            ChangeTarget.ForPark(park.Id!),
            previousValue,
            newValue,
            source,
            DataConfidence.High,
            verifiedAtUtc,
            $"park:{park.Id}:opening-calendar");
    }

    public async Task CaptureAsync(
        ParkOpeningHoursPendingFactualChange pendingFactualChange,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendingFactualChange);
        FactualChangeCaptureResult result =
            await this.factualChangeCaptureService.CapturePreparedAfterCommitAsync(
                pendingFactualChange.Entry,
                cancellationToken);
        if (result.Disposition == FactualChangeCaptureDisposition.Conflict
            || result.Disposition == FactualChangeCaptureDisposition.NoChange)
        {
            throw new InvalidOperationException(
                $"The pending factual change '{pendingFactualChange.Entry.Id}' could not be recorded safely ({result.Disposition}).");
        }

        try
        {
            _ = await this.openingHoursRepository.MarkFactualChangeRecordedAsync(
                pendingFactualChange.ParkId,
                pendingFactualChange.Entry.Id,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Factual change {OutboxEntryId} is durable but its opening-hours source marker could not be cleared.",
                pendingFactualChange.Entry.Id);
        }
    }

    public async Task<ParkOpeningHoursFactualChangeCursor?> ReconcilePendingAsync(
        ParkOpeningHoursFactualChangeCursor? after,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkOpeningHoursPendingFactualChange> pendingChanges =
            await this.openingHoursRepository.GetPendingFactualChangesAsync(
                after,
                maximumCount,
                cancellationToken);
        foreach (ParkOpeningHoursPendingFactualChange pendingChange in pendingChanges)
        {
            try
            {
                await this.CaptureAsync(pendingChange, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to reconcile opening-hours factual change {OutboxEntryId}; its source marker remains durable.",
                    pendingChange.Entry.Id);
            }
        }

        if (pendingChanges.Count < maximumCount)
        {
            return null;
        }

        ParkOpeningHoursPendingFactualChange lastChange = pendingChanges.Last();
        return new ParkOpeningHoursFactualChangeCursor(
            lastChange.SourceUpdatedAtUtc,
            lastChange.ParkId,
            lastChange.Entry.RecordedAtUtc,
            lastChange.Entry.Id);
    }

    private static bool CanCapture(Park park, ParkOpeningHoursSchedule schedule)
    {
        string parkName = park.Name?.Trim() ?? string.Empty;
        string sourceTitle = $"Calendrier officiel — {parkName}";
        string sourceUrl = schedule.SourceUrl?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(park.Id)
            || parkName.Length == 0
            || parkName.Length > SourceReference.MaximumPublisherNameLength
            || parkName.Any(char.IsControl)
            || sourceTitle.Length > SourceReference.MaximumTitleLength
            || sourceUrl.Length == 0
            || sourceUrl.Length > SourceReference.MaximumUrlLength
            || sourceUrl.Any(char.IsControl)
            || !schedule.LastVerifiedAtUtc.HasValue
            || schedule.LastVerifiedAtUtc.Value.Kind != DateTimeKind.Utc)
        {
            return false;
        }

        return Uri.TryCreate(sourceUrl, UriKind.Absolute, out Uri? sourceUri)
            && (sourceUri.Scheme == Uri.UriSchemeHttp
                || sourceUri.Scheme == Uri.UriSchemeHttps)
            && sourceUri.AbsoluteUri.Length <= SourceReference.MaximumUrlLength;
    }
}
