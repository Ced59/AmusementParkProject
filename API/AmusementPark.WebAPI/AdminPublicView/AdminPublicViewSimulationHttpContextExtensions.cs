using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.AdminPublicView;

internal static class AdminPublicViewSimulationHttpContextExtensions
{
    public static bool UserCanSeeNonVisibleInPublicView(this HttpContext context)
    {
        return AdminPublicViewSimulation.CanSeeNonVisible(context);
    }
}
