using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.ParkOpeningHours.Models;

public sealed record ParkOpeningHoursFactualChangeDraft(
    FactualEventType Type,
    ChangeTarget Target,
    FactValue? PreviousValue,
    FactValue? NewValue,
    SourceReference Source,
    DataConfidence Confidence,
    DateTime OccurredAtUtc,
    string DeduplicationKey)
{
    public FactualChangeCaptureRequest ToCaptureRequest(
        long sourceRevision,
        DateTime recordedAtUtc)
    {
        return new FactualChangeCaptureRequest(
            this.Type,
            this.Target,
            this.PreviousValue,
            this.NewValue,
            this.Source,
            this.Confidence,
            this.OccurredAtUtc,
            this.DeduplicationKey,
            sourceRevision,
            recordedAtUtc);
    }
}
