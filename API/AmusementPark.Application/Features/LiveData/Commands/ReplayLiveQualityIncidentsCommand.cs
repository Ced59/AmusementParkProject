using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;

namespace AmusementPark.Application.Features.LiveData.Commands;

public sealed record ReplayLiveQualityIncidentsCommand(
    int MaximumCount,
    string AdministratorUserId)
    : ICommand<ApplicationResult<LiveQualityReplayResult>>;
