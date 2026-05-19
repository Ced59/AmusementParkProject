using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Type HTTP détaillé d'un park item.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParkItemTypeDto
{
    Attraction,
    RollerCoaster,
    WaterRide,
    FlatRide,
    DarkRide,
    FamilyRide,
    ThrillRide,
    TransportRide,
    WalkThrough,
    Playground,
    InteractiveExperience,
    ObservationRide,
    AnimalExhibit,
    Restaurant,
    Snack,
    Hotel,
    Show,
    Shop,
    Game,
    MeetAndGreet,
    Service,
    Toilets,
    FirstAid,
    Information,
    Locker,
    Parking,
    Transport,
    Station,
    Other,
}
