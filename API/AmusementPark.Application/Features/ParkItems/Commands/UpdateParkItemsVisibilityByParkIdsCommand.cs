using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkItems.Commands;

public sealed record UpdateParkItemsVisibilityByParkIdsCommand(
    IReadOnlyCollection<string> ParkIds,
    bool IsVisible) : ICommand<ApplicationResult<BulkAdministrationUpdateResult>>;
