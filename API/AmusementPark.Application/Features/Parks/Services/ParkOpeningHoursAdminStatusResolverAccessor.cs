using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Services;

internal sealed class ParkOpeningHoursAdminStatusResolverAccessor
{
    private readonly Func<ParkOpeningHoursScheduleSummary, ParkOpeningHoursAdminStatus> resolveStatus;

    public ParkOpeningHoursAdminStatusResolverAccessor(Func<ParkOpeningHoursScheduleSummary, ParkOpeningHoursAdminStatus> resolveStatus)
    {
        this.resolveStatus = resolveStatus;
    }

    public ParkOpeningHoursAdminStatus ResolveStatus(ParkOpeningHoursScheduleSummary summary)
    {
        return this.resolveStatus(summary);
    }
}
