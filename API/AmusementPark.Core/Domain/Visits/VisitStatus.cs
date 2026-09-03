namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// État explicite d'une visite privée.
/// </summary>
public enum VisitStatus
{
    Draft = 1,
    Completed = 2,
    Archived = 3,
}
