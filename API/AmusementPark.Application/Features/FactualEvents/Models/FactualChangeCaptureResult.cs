namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record FactualChangeCaptureResult(
    FactualChangeCaptureDisposition Disposition,
    string? OutboxEntryId,
    string? EventId);
