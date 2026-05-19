using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Common;

/// <summary>
/// Statut de traitement interne exposé aux écrans d'administration.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AdminReviewStatusDto
{
    Ready,
    ToProcessLater,
}
