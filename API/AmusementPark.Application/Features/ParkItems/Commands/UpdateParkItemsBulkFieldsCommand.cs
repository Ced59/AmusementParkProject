using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Commands;

/// <summary>
/// Action de masse sur les champs metier rapides des park items.
/// </summary>
public sealed record UpdateParkItemsBulkFieldsCommand(
    IReadOnlyCollection<string> ParkItemIds,
    bool UpdateZone,
    string? ZoneId,
    ParkItemCategory? Category,
    ParkItemType? Type,
    bool UpdateManufacturer,
    string? ManufacturerId,
    bool? IsVisible,
    AdminReviewStatus? AdminReviewStatus) : ICommand<ApplicationResult<BulkAdministrationUpdateResult>>;
