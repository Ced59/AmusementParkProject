using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Common;

/// <summary>
/// Statut de revue interne exposé aux écrans d'administration.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AdminReviewStatusDto
{
    ToReview,
    Validated,
    ToProcessLater,
    NotRelevant,

    /// <summary>
    /// Ancienne valeur M14 conservée en entrée pour éviter une rupture brutale des clients.
    /// </summary>
    Ready,
}
