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

    public string? AuthorAvatarUrl { get; set; }

    public Role AuthorRole { get; set; }

    public List<LocalizedText> Bodies { get; set; } = new List<LocalizedText>();

    public List<string> ImageIds { get; set; } = new List<string>();

    public long Revision { get; set; }

    public bool IsOfficial { get; set; }

    public CommentModerationStatus ModerationStatus { get; set; } = CommentModerationStatus.Published;

    public void UpdateContent(
        IReadOnlyCollection<LocalizedText> bodies,
        IReadOnlyCollection<string> imageIds,
        bool isOfficial)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        ArgumentNullException.ThrowIfNull(imageIds);

        this.Bodies = bodies.ToList();
        this.ImageIds = imageIds.Distinct(StringComparer.Ordinal).ToList();
        this.IsOfficial = isOfficial;
        this.Touch();
    }

    /// <summary>
    /// Indique si un utilisateur peut administrer ce commentaire.
    /// Un administrateur peut gérer tous les commentaires ; les autres utilisateurs uniquement les leurs.
    /// </summary>
    public bool CanBeManagedBy(User actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return actor.HasRole(Role.Admin)
            || string.Equals(actor.Id, this.AuthorUserId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Indique si l'utilisateur peut attribuer ou retirer le statut officiel.
    /// </summary>
    public static bool CanManageOfficialStatus(User actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return actor.HasRole(Role.Admin) || actor.HasRole(Role.Moderator);
    }
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
