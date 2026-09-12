namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursTimeRange
{
    public TimeOnly OpensAt { get; set; }

    public TimeOnly ClosesAt { get; set; }

    public bool ClosesNextDay { get; set; }

    public TimeOnly? LastAdmissionAt { get; set; }

    public bool LastAdmissionNextDay { get; set; }
}
