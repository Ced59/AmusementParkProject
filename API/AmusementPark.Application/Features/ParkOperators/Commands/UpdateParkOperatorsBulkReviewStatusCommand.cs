using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Commands;

/// <summary>
/// Met à jour en masse le statut de revue admin des exploitants.
/// </summary>
public sealed record UpdateParkOperatorsBulkReviewStatusCommand(
    IReadOnlyCollection<string> Ids,
    AdminReviewStatus AdminReviewStatus) : ICommand<ApplicationResult<BulkAdministrationUpdateResult>>;
