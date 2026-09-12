using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Commands;

public sealed record UpsertHistoryEventCommand(HistoryEventWriteModel Event) : ICommand<ApplicationResult<HistoryEvent>>;
