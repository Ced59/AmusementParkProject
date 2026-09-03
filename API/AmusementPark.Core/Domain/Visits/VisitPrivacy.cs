namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Niveau de visibilité d'une visite. Seul Private est activé dans le Passeport V1.
/// </summary>
public enum VisitPrivacy
{
    Private = 1,
    Unlisted = 2,
    Public = 3,
}
