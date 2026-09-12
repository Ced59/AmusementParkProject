namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportVisitExperience(
    string VisitId,
    string ParkId,
    VisitDate VisitDate);
