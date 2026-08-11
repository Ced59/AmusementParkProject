using AmusementPark.Application.Features.SocialPublishing.Contracts;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

internal static class SocialPublicationMessageBuilder
{
    private const string ParkHashtags =
        "#AmusementParks #ParcAttractions #MontagnesRusses #PassionParcs #Tourisme "
        + "#ThemeParks #RollerCoasters #Travel";

    public static string BuildDefaultMessage(ResolvedSocialPublicationTarget target)
    {
        return target.Kind switch
        {
            SocialPublicationTargetKind.Park => BuildParkAnnouncementMessage(
                target.FrenchName,
                target.EnglishName,
                target.Url),
            SocialPublicationTargetKind.ParkItem =>
                $"🎢 Une nouvelle fiche est disponible sur Amusement Parks : {target.FrenchName} !\n"
                + "Découvre-la dès maintenant.\n\n"
                + $"🇬🇧 A new page is now available on Amusement Parks: {target.EnglishName}!\n"
                + "Discover it now.",
            SocialPublicationTargetKind.Video =>
                $"🎬 Une vidéo est à découvrir sur Amusement Parks : {target.FrenchName} !\n"
                + "Regarde-la dès maintenant.\n\n"
                + $"🇬🇧 A video is waiting for you on Amusement Parks: {target.EnglishName}!\n"
                + "Watch it now.",
            _ =>
                $"🎢 À découvrir sur Amusement Parks : {target.FrenchName} !\n"
                + "Consulte cette page dès maintenant.\n\n"
                + $"🇬🇧 Discover this on Amusement Parks: {target.EnglishName}!\n"
                + "Explore the page now.",
        };
    }

    public static string BuildParkAnnouncementMessage(
        string frenchParkName,
        string englishParkName,
        Uri parkUrl)
    {
        ArgumentNullException.ThrowIfNull(parkUrl);

        UriBuilder canonicalUrlBuilder = new UriBuilder(parkUrl)
        {
            Fragment = string.Empty,
            Query = string.Empty,
        };
        string canonicalUrl = canonicalUrlBuilder.Uri.AbsoluteUri;

        return $"🌍 Nouveau parc ajouté sur Amusement-Parks.Fun : {frenchParkName.Trim()} ! 🎢\n\n"
            + "Tu connais ce parc ? Tu l’as déjà visité ? Qu’en as-tu pensé ? Dis-le-nous en commentaire ! 😊\n\n"
            + $"🇬🇧 New park added to Amusement-Parks.Fun: {englishParkName.Trim()}! 🎢\n\n"
            + "Do you know this park? Have you visited it? What did you think? Tell us in the comments! 😊\n\n"
            + "🔗 Découvre le parc ici / Discover the park here:\n"
            + $"{canonicalUrl}\n\n"
            + ParkHashtags;
    }
}
