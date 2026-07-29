using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Comments;

/// <summary>
/// Commentaire éditorial rattaché à un parc ou à un élément de parc.
/// </summary>
public sealed class Comment : AuditableEntity
{
    public CommentTargetType TargetType { get; set; }

    public string TargetId { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string AuthorUserId { get; set; } = string.Empty;

    public string AuthorDisplayName { get; set; } = string.Empty;

    public Role AuthorRole { get; set; }

    public List<LocalizedText> Bodies { get; set; } = new List<LocalizedText>();

    public bool IsOfficial { get; set; }

    public CommentModerationStatus ModerationStatus { get; set; } = CommentModerationStatus.Published;
}

/// <summary>
/// Type de contenu pouvant recevoir un commentaire.
/// </summary>
public enum CommentTargetType
{
    Park = 1,
    ParkItem = 2,
}

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
