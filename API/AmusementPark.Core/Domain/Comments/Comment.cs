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

    public bool IsOfficial { get; set; }

    public CommentModerationStatus ModerationStatus { get; set; } = CommentModerationStatus.Published;

    public void UpdateContent(IReadOnlyCollection<LocalizedText> bodies, bool isOfficial)
    {
        ArgumentNullException.ThrowIfNull(bodies);

        this.Bodies = bodies.ToList();
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
