using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class ProfileComparisonPassportDataMapper
{
    public static ProfileComparisonPassportData ToComparisonData(
        this ProfileComparisonPassportReference passport)
    {
        ArgumentNullException.ThrowIfNull(passport);
        PassportProfileSharePreviewResult content = passport.Snapshot.Content;
        return new ProfileComparisonPassportData(
            content.DisplayName,
            content.Parks.Select(static park => new ProfileComparisonParkData(
                park.Name,
                park.CountryCode,
                park.VisitCount)).ToArray(),
            content.PersonalRanking.Select(static rating => new ProfileComparisonRatingData(
                rating.TargetType,
                rating.Name,
                rating.ParkName,
                rating.Category,
                rating.Rating)).ToArray(),
            content.Years.Select(static year => new ProfileComparisonYearData(
                year.Year,
                year.VisitCount,
                year.CompletedRideCount)).ToArray(),
            content.MissedItems.Select(static item => new ProfileComparisonMissedItemData(
                item.Name,
                item.Status,
                item.OccurrenceCount)).ToArray(),
            content.HasIncompleteCatalog);
    }
}
