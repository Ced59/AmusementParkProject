namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record FactualChangeMaterializationJobPayload(
    string OutboxEntryId,
    long SourceRevision);
