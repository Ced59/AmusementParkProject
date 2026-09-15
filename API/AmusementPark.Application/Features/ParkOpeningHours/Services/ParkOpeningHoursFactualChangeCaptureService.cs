using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Parks;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursFactualChangeCaptureService
    : IParkOpeningHoursFactualChangeCapture
{
    private readonly IFactualChangeCaptureService factualChangeCaptureService;
    private readonly ILogger<ParkOpeningHoursFactualChangeCaptureService> logger;

    public ParkOpeningHoursFactualChangeCaptureService(
        IFactualChangeCaptureService factualChangeCaptureService,
        ILogger<ParkOpeningHoursFactualChangeCaptureService> logger)
    {
        this.factualChangeCaptureService = factualChangeCaptureService;
        this.logger = logger;
    }

    public async Task CaptureAsync(
        Park park,
        ParkOpeningHoursSchedule? previousSchedule,
        ParkOpeningHoursSchedule currentSchedule,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(park);
        ArgumentNullException.ThrowIfNull(currentSchedule);
        if (!CanCapture(park, currentSchedule))
        {
            return;
        }

        FactValue? previousValue = OpeningCalendarFactSnapshot.Create(previousSchedule);
        FactValue? newValue = OpeningCalendarFactSnapshot.Create(currentSchedule);
        if (newValue is null)
        {
            return;
        }

        string parkName = park.Name!.Trim();
        DateTime verifiedAtUtc = currentSchedule.LastVerifiedAtUtc!.Value;
        try
        {
            SourceReference source = new SourceReference(
                SourceReferenceType.OfficialWebsite,
                parkName,
                $"Calendrier officiel — {parkName}",
                currentSchedule.SourceUrl!,
                verifiedAtUtc);
            FactualChangeCaptureRequest request = new FactualChangeCaptureRequest(
                previousValue is null
                    ? FactualEventType.OpeningCalendarPublished
                    : FactualEventType.OpeningCalendarChanged,
                ChangeTarget.ForPark(park.Id!),
                previousValue,
                newValue,
                source,
                DataConfidence.High,
                verifiedAtUtc,
                $"park:{park.Id}:opening-calendar",
                currentSchedule.UpdatedAtUtc.Ticks,
                currentSchedule.UpdatedAtUtc);
            await this.factualChangeCaptureService.CaptureAfterCommitAsync(
                request,
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
                "Unable to capture the opening-calendar factual change for park {ParkId} after commit.",
                park.Id);
        }
    }

    private static bool CanCapture(Park park, ParkOpeningHoursSchedule schedule)
    {
        if (string.IsNullOrWhiteSpace(park.Id)
            || string.IsNullOrWhiteSpace(park.Name)
            || string.IsNullOrWhiteSpace(schedule.SourceUrl)
            || !schedule.LastVerifiedAtUtc.HasValue
            || schedule.LastVerifiedAtUtc.Value.Kind != DateTimeKind.Utc
            || schedule.UpdatedAtUtc.Kind != DateTimeKind.Utc
            || schedule.UpdatedAtUtc.Ticks < 1)
        {
            return false;
        }

        return Uri.TryCreate(schedule.SourceUrl.Trim(), UriKind.Absolute, out Uri? sourceUri)
            && (sourceUri.Scheme == Uri.UriSchemeHttp
                || sourceUri.Scheme == Uri.UriSchemeHttps);
    }
}
