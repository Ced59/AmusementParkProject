using AmusementPark.Application.Features.SocialPublishing.Contracts;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

internal static class SocialPublicationMessageBuilder
{
    public static string BuildDefaultMessage(ResolvedSocialPublicationTarget target)
    {
        return target.Kind switch
        {
            SocialPublicationTargetKind.Park => SocialPublicationService.BuildParkAnnouncementMessage(target.FrenchName),
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
}
