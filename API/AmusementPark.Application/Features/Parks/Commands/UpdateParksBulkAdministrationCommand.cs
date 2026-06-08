using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Commands;

/// <summary>
/// Action de masse d'administration sur les parcs.
/// </summary>
public sealed record UpdateParksBulkAdministrationCommand(
    IReadOnlyCollection<string> ParkIds,
    bool? IsVisible,
    AdminReviewStatus? AdminReviewStatus,
    bool? FilterIsVisible = null,
    AdminReviewStatus? FilterAdminReviewStatus = null,
    ParkType? FilterType = null,
    string? FilterCountryCode = null,
    bool? FilterHasValidCoordinates = null) : ICommand<ApplicationResult<BulkAdministrationUpdateResult>>;
