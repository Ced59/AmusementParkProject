using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Models;

public sealed record HistoryEventMutationSnapshot(
    HistoryEvent HistoryEvent,
    Guid? LastMutationId);
