using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Comments;

public static class CommentApplicationErrors
{
    public static ApplicationError InvalidTargetType()
    {
        return ApplicationError.Validation(
            "comment.target-type.invalid",
            "La cible du commentaire est invalide.");
    }

    public static ApplicationError TargetNotFound()
    {
        return ApplicationError.NotFound(
            "comment.target.not-found",
            "La cible du commentaire est introuvable.");
    }

    public static ApplicationError CommentNotFound()
    {
        return ApplicationError.NotFound(
            "comment.not-found",
            "Le commentaire est introuvable.");
    }

    public static ApplicationError AuthorNotAllowed()
    {
        return ApplicationError.Forbidden(
            "comment.author.forbidden",
            "Seuls les administrateurs et les modérateurs peuvent publier un commentaire.");
    }

    public static ApplicationError ManagerNotAllowed()
    {
        return ApplicationError.Forbidden(
            "comment.manager.forbidden",
            "Seuls les administrateurs ou l’auteur du commentaire peuvent le modifier ou le supprimer.");
    }

    public static ApplicationError InvalidLanguage()
    {
        return ApplicationError.Validation(
            "comment.language.invalid",
            "Une langue du commentaire n'est pas prise en charge.");
    }

    public static ApplicationError EmptyBody()
    {
        return ApplicationError.Validation(
            "comment.body.empty",
            "Le commentaire doit contenir du texte dans au moins une langue.");
    }

    public static ApplicationError BodyTooLong()
    {
        return ApplicationError.Validation(
            "comment.body.too-long",
            "Le commentaire dépasse la longueur maximale autorisée.");
    }
}
