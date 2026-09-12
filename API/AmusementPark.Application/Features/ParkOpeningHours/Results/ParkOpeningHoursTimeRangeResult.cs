using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkOpeningHours.Results;

public sealed class ParkOpeningHoursTimeRangeResult
{
    public TimeOnly OpensAt { get; init; }

    public TimeOnly ClosesAt { get; init; }

    public bool ClosesNextDay { get; init; }

    public TimeOnly? LastAdmissionAt { get; init; }

    public bool LastAdmissionNextDay { get; init; }
}
