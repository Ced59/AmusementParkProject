namespace AmusementPark.Application.Features.Parks.Contracts;

/// <summary>
/// Filtre de rayonnement des parcs, avec une valeur applicative pour les parcs non renseignés.
/// </summary>
public enum ParkAudienceClassificationFilter
{
    International,
    National,
    Regional,
    Local,
    Unspecified,
}
