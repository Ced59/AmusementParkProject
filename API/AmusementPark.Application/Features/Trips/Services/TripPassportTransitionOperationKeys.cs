using System.Security.Cryptography;
using System.Text;

namespace AmusementPark.Application.Features.Trips.Services;

internal static class TripPassportTransitionOperationKeys
{
    public static string Visit(string tripPlanId, string userId, DateOnly localDate)
    {
        return Build("trip-passport-visit", tripPlanId, userId, localDate);
    }

    public static string Rides(string tripPlanId, string userId, DateOnly localDate)
    {
        return Build("trip-passport-rides", tripPlanId, userId, localDate);
    }

    private static string Build(
        string prefix,
        string tripPlanId,
        string userId,
        DateOnly localDate)
    {
        string source = string.Join(
            '\n',
            tripPlanId,
            userId,
            localDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        string digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
        return $"{prefix}:{digest}";
    }
}
