using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record FactualChangeOutboxEntry(
    string Id,
    string EventId,
    FactualEventType Type,
    int DefinitionVersion,
    ChangeTarget Target,
    FactValue? PreviousValue,
    FactValue? NewValue,
    SourceReference Source,
    DataConfidence Confidence,
    DateTime OccurredAtUtc,
    string DeduplicationKey,
    long SourceRevision,
    DateTime RecordedAtUtc,
    DateTime? MaterializedAtUtc,
    DateTime? TerminalAtUtc,
    string? TerminalErrorCode,
    long Version)
{
    public bool IsMaterialized => this.MaterializedAtUtc.HasValue;

    public bool IsTerminal => this.TerminalAtUtc.HasValue;

    public static FactualChangeOutboxEntry? Create(
        FactualChangeCaptureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SourceRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "The source revision must be positive.");
        }

        FactualChangeDiff? diff = FactualChangeDiff.Detect(
            request.PreviousValue,
            request.NewValue);
        if (diff is null)
        {
            return null;
        }

        FactualChangeEvent candidate = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.New(),
            request.Type,
            request.Target,
            diff.PreviousValue,
            diff.NewValue,
            request.Source,
            request.Confidence,
            request.OccurredAtUtc,
            request.DeduplicationKey,
            request.SourceRevision,
            request.RecordedAtUtc);
        return new FactualChangeOutboxEntry(
            Guid.NewGuid().ToString("N"),
            candidate.Id.Value,
            candidate.Type,
            candidate.DefinitionVersion,
            candidate.Target,
            candidate.PreviousValue,
            candidate.NewValue,
            candidate.Source,
            candidate.Confidence,
            candidate.OccurredAtUtc,
            candidate.DeduplicationKey,
            request.SourceRevision,
            candidate.CreatedAtUtc,
            null,
            null,
            null,
            1);
    }
}
