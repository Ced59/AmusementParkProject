using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Comments;

/// <summary>
/// Type de contenu pouvant recevoir un commentaire.
/// </summary>
public enum CommentTargetType
{
    Park = 1,
    ParkItem = 2,
}
