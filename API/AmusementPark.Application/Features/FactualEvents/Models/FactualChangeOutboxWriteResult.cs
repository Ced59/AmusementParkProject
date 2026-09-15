namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record FactualChangeOutboxWriteResult(
    FactualChangeOutboxWriteDisposition Disposition,
    FactualChangeOutboxEntry? Entry);
