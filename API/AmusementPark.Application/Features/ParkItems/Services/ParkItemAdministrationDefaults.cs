using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkItems.Services;

/// <summary>
/// Valeurs par defaut des flux d'administration rapides des park items.
/// </summary>
public static class ParkItemAdministrationDefaults
{
    public const bool QuickCreateIsVisible = false;
    public const AdminReviewStatus QuickCreateAdminReviewStatus = AdminReviewStatus.ToReview;
    public const ParkItemCategory QuickCreateCategory = ParkItemCategory.Attraction;
    public const ParkItemType QuickCreateType = ParkItemType.Attraction;

    public static void ApplyQuickCreateDefaults(ParkItem parkItem, GeoPoint? fallbackPosition = null)
    {
        ArgumentNullException.ThrowIfNull(parkItem);

        parkItem.Descriptions ??= new List<LocalizedText>();
        if (!IsTypeAllowedForCategory(parkItem.Category, parkItem.Type))
        {
            parkItem.Type = GetDefaultType(parkItem.Category);
        }

        if (parkItem.Position is null && fallbackPosition is not null)
        {
            parkItem.SetPosition(fallbackPosition);
        }
    }

    public static ParkItemType GetDefaultType(ParkItemCategory category)
    {
        return category switch
        {
            ParkItemCategory.Restaurant => ParkItemType.Restaurant,
            ParkItemCategory.Hotel => ParkItemType.Hotel,
            ParkItemCategory.Animal => ParkItemType.AnimalExhibit,
            ParkItemCategory.Show => ParkItemType.Show,
            ParkItemCategory.Shop => ParkItemType.Shop,
            ParkItemCategory.Service => ParkItemType.Service,
            ParkItemCategory.Transport => ParkItemType.Transport,
            ParkItemCategory.Other => ParkItemType.Other,
            _ => QuickCreateType,
        };
    }

    private static bool IsTypeAllowedForCategory(ParkItemCategory category, ParkItemType type)
    {
        return category switch
        {
            ParkItemCategory.Attraction => type is ParkItemType.Attraction
                or ParkItemType.RollerCoaster
                or ParkItemType.WaterRide
                or ParkItemType.FlatRide
                or ParkItemType.DarkRide
                or ParkItemType.FamilyRide
                or ParkItemType.ThrillRide
                or ParkItemType.TransportRide
                or ParkItemType.WalkThrough
                or ParkItemType.Playground
                or ParkItemType.InteractiveExperience
                or ParkItemType.Cinema
                or ParkItemType.Game
                or ParkItemType.MeetAndGreet
                or ParkItemType.ObservationRide
                or ParkItemType.Other,
            ParkItemCategory.Restaurant => type is ParkItemType.Restaurant or ParkItemType.Snack,
            ParkItemCategory.Hotel => type == ParkItemType.Hotel,
            ParkItemCategory.Animal => type == ParkItemType.AnimalExhibit,
            ParkItemCategory.Show => type == ParkItemType.Show,
            ParkItemCategory.Shop => type == ParkItemType.Shop,
            ParkItemCategory.Service => type is ParkItemType.Service
                or ParkItemType.Toilets
                or ParkItemType.FirstAid
                or ParkItemType.Information
                or ParkItemType.Locker
                or ParkItemType.Parking,
            ParkItemCategory.Transport => type is ParkItemType.Transport or ParkItemType.Station,
            ParkItemCategory.Other => type == ParkItemType.Other,
            _ => false,
        };
    }
}
