using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Commands;

/// <summary>
/// Action de masse d'administration sur les éléments de parc.
/// </summary>
public sealed record UpdateParkItemsBulkAdministrationCommand(
    IReadOnlyCollection<string> ParkItemIds,
    bool? IsVisible,
    AdminReviewStatus? AdminReviewStatus) : ICommand<ApplicationResult<BulkAdministrationUpdateResult>>;
