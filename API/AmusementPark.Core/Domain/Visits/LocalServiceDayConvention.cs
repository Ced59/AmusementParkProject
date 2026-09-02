namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Convention utilisée pour rattacher une visite à son jour de service local.
/// </summary>
public enum LocalServiceDayConvention
{
    VisitStartLocalDate = 1,
    UserSelectedServiceDate = 2,
}
