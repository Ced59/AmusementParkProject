using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

/// <summary>
/// Récupère une page de parcs.
/// </summary>
/// <param name="Paging">Paramètres de pagination.</param>
/// <param name="IncludeHidden">Indique si les parcs non visibles doivent être inclus.</param>
/// <param name="IsVisible">Filtre explicite sur la visibilite.</param>
/// <param name="AdminReviewStatus">Filtre sur le statut de revue admin.</param>
/// <param name="Type">Filtre sur le type de parc.</param>
/// <param name="AudienceClassificationFilter">Filtre sur le rayonnement du parc.</param>
/// <param name="CountryCode">Filtre sur le code pays.</param>
/// <param name="HasValidCoordinates">Filtre les parcs selon la presence de coordonnees valides.</param>
/// <param name="ClosedFilter">Filtre les parcs ouverts ou fermes.</param>
/// <param name="OpeningHoursFilter">Filtre selon l'etat de couverture des horaires.</param>
/// <param name="SortField">Champ de tri.</param>
/// <param name="SortDescending">Indique si le tri doit etre descendant.</param>
public sealed record GetParksPageQuery(
    PagedQuery Paging,
    bool IncludeHidden = false,
    bool? IsVisible = null,
    AdminReviewStatus? AdminReviewStatus = null,
    ParkType? Type = null,
    ParkAudienceClassificationFilter? AudienceClassificationFilter = null,
    string? CountryCode = null,
    bool? HasValidCoordinates = null,
    ClosedEntityFilter ClosedFilter = ClosedEntityFilter.OpenOnly,
    ParkOpeningHoursAdminFilter OpeningHoursFilter = ParkOpeningHoursAdminFilter.All,
    ParkAdminSortField SortField = ParkAdminSortField.Default,
    bool SortDescending = false) : IQuery<ApplicationResult<PagedResult<ParkListResult>>>;
