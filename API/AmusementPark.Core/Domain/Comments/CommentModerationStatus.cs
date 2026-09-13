using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Comments;

/// <summary>
/// État éditorial prévu pour l'ouverture future des commentaires aux utilisateurs.
/// Les commentaires d'administrateurs et de modérateurs sont publiés directement.
/// </summary>
public enum CommentModerationStatus
{
    PendingReview = 1,
    Published = 2,
    Rejected = 3,
}
