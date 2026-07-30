using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Images;

/// <summary>
/// Agrégat métier représentant une image.
/// </summary>
public sealed class Image : AuditableEntity
{
    /// <summary>
    /// Catégorie fonctionnelle.
    /// </summary>
    public ImageCategory Category { get; set; }

    /// <summary>
    /// Chemin technique ou objet distant.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Description libre.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Textes alternatifs localisés.
    /// </summary>
    public List<LocalizedText> AltTexts { get; set; } = new();

    /// <summary>
    /// Légendes localisées.
    /// </summary>
    public List<LocalizedText> Captions { get; set; } = new();

    /// <summary>
    /// Crédits localisés.
    /// </summary>
    public List<LocalizedText> Credits { get; set; } = new();

    /// <summary>
    /// Identifiants des tags associés.
    /// </summary>
    public List<string> TagIds { get; set; } = new();

    /// <summary>
    /// Position GPS extraite si disponible.
    /// </summary>
    public GeoPoint? GeoLocation { get; set; }

    /// <summary>
    /// Métadonnées EXIF extraites.
    /// </summary>
    public ImageExifMetadata? ExifMetadata { get; set; }

    /// <summary>
    /// Largeur en pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Hauteur en pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Taille du fichier en octets.
    /// </summary>
    public long SizeInBytes { get; set; }

    /// <summary>
    /// Type de propriétaire.
    /// </summary>
    public ImageOwnerType OwnerType { get; set; } = ImageOwnerType.None;

    /// <summary>
    /// Identifiant du propriétaire éventuel.
    /// </summary>
    public string? OwnerId { get; set; }

    /// <summary>
    /// Indique si l'image est l'image courante du propriétaire.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Nom de fichier original éventuel.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Content type MIME.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// URL source externe d'origine lorsque l'image a ete importee depuis le web.
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Indique si le watermark applicatif a ete applique au binaire stocke.
    /// </summary>
    public bool IsWatermarked { get; set; }

    /// <summary>
    /// Indique si l'image est publiée.
    /// </summary>
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// Propriétaire initial d'un brouillon de commentaire. Cette valeur permet
    /// de rendre le brouillon à son auteur si l'association au commentaire échoue.
    /// </summary>
    public string? DraftOwnerId { get; set; }

    /// <summary>
    /// Commentaire auquel le brouillon est réservé pendant l'écriture Mongo.
    /// Une image réservée reste privée jusqu'à la finalisation.
    /// </summary>
    public string? PendingCommentId { get; set; }

    /// <summary>
    /// Identifie la tentative applicative qui détient la réservation du brouillon.
    /// </summary>
    public string? PendingReservationToken { get; set; }

    /// <summary>
    /// Révision du commentaire que la réservation attend avant d'être libérée.
    /// </summary>
    public long? PendingCommentRevision { get; set; }

    /// <summary>
    /// Échéance dure après laquelle une réservation sans commentaire visible
    /// peut être libérée par la réconciliation.
    /// </summary>
    public DateTime? PendingReservationExpiresAtUtc { get; set; }

    /// <summary>
    /// Tokens de tentatives explicitement annulées. Une écriture Mongo tardive
    /// portant l'un de ces tokens ne peut plus réserver le brouillon.
    /// </summary>
    public List<string> AbortedReservationTokens { get; set; } = new();

    /// <summary>
    /// Date à partir de laquelle une suppression demandée peut être évaluée.
    /// </summary>
    public DateTime? CleanupRequestedAtUtc { get; set; }

    /// <summary>
    /// Révision que le commentaire doit avoir atteinte avant d'évaluer
    /// cette demande de suppression. L'absence du commentaire satisfait
    /// également cette barrière.
    /// </summary>
    public long? CleanupCommentRevision { get; set; }

    /// <summary>
    /// Date technique à partir de laquelle une réservation doit être réconciliée.
    /// </summary>
    public DateTime? ReservationReconcileAfterUtc { get; set; }

    public string? CommentReuseReservationToken { get; set; }

    public DateTime? CommentReuseReconcileAfterUtc { get; set; }

    public long? CommentReuseTargetRevision { get; set; }

    public bool CanBeUsedInComment(string actorUserId, string commentId)
    {
        bool isPublishedForComment =
            this.Category == ImageCategory.Comment
            && this.OwnerType == ImageOwnerType.Comment
            && string.Equals(this.OwnerId, commentId, StringComparison.Ordinal)
            && this.IsPublished;
        bool isDraftOwnedByActor =
            this.Category == ImageCategory.Comment
            && this.OwnerType == ImageOwnerType.CommentDraft
            && string.Equals(this.OwnerId, actorUserId, StringComparison.Ordinal)
            && (string.IsNullOrWhiteSpace(this.PendingCommentId)
                || string.Equals(this.PendingCommentId, commentId, StringComparison.Ordinal))
            && !this.IsPublished;
        return isPublishedForComment || isDraftOwnedByActor;
    }

    public bool IsCommentDraftOwnedBy(string actorUserId)
    {
        return this.Category == ImageCategory.Comment
            && this.OwnerType == ImageOwnerType.CommentDraft
            && string.Equals(this.OwnerId, actorUserId, StringComparison.Ordinal)
            && !this.IsPublished;
    }

    public bool IsOwnedByComment(string commentId)
    {
        return this.Category == ImageCategory.Comment
            && this.OwnerType == ImageOwnerType.Comment
            && string.Equals(this.OwnerId, commentId, StringComparison.Ordinal)
            && this.IsPublished;
    }
}
