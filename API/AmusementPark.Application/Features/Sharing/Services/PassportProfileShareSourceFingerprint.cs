using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class PassportProfileShareSourceFingerprint
{
    public static string Create(
        PassportProfileSourceData source,
        PassportProfileShareInput input)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(input);
        HashSet<int> selectedYears = new HashSet<int>(
            input.SelectedYears ?? Array.Empty<int>());
        HashSet<string> selectedParkIds = new HashSet<string>(
            input.SelectedParkIds ?? Array.Empty<string>(),
            StringComparer.Ordinal);
        PassportVisitStatisticsObservation[] visits = source.Visits
            .Where(visit => selectedYears.Contains(visit.VisitDate.Year)
                && selectedParkIds.Contains(visit.ParkId))
            .OrderBy(static visit => visit.VisitId, StringComparer.Ordinal)
            .ToArray();
        HashSet<string> selectedVisitIds = visits
            .Select(static visit => visit.VisitId)
            .ToHashSet(StringComparer.Ordinal);
        PassportRideStatisticsObservation[] rides = source.Rides
            .Where(ride => selectedVisitIds.Contains(ride.VisitId))
            .OrderBy(static ride => ride.RideOccurrenceId, StringComparer.Ordinal)
            .ToArray();
        StringBuilder canonical = new StringBuilder();
        Append(canonical, PassportProfileShareVersion.CalculationVersion);
        foreach (PassportVisitStatisticsObservation visit in visits)
        {
            Append(canonical, "visit");
            Append(canonical, visit.VisitId);
            Append(canonical, visit.ParkId);
            Append(canonical, visit.VisitDate.Year);
            Append(canonical, visit.VisitDate.Month);
            Append(canonical, visit.VisitDate.Day);
            Append(canonical, visit.VisitDate.Precision.ToString());
            Append(canonical, visit.VisitDate.IsApproximate);
            Append(canonical, visit.ParkAssessment?.HalfSteps);
        }

        foreach (PassportRideStatisticsObservation ride in rides)
        {
            Append(canonical, "ride");
            Append(canonical, ride.RideOccurrenceId);
            Append(canonical, ride.VisitId);
            Append(canonical, ride.ParkId);
            Append(canonical, ride.ParkItemId);
            Append(canonical, ride.Status.ToString());
            Append(canonical, ride.Assessment?.HalfSteps);
            Append(canonical, ride.HistoricalName);
            Append(canonical, ride.HistoricalCategory);
            Append(canonical, ride.CurrentCategory);
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static void Append(StringBuilder target, object? value)
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        target.Append(text.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(text);
    }
}
