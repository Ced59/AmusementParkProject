using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Services;
using AmusementPark.Core.Domain.Images;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialPublishing.Services;

public sealed class SocialPublicationMessageBuilderTests
{
    [Fact]
    public void BuildDefaultMessage_ForPark_ShouldCreateBilingualFacebookPostWithCanonicalLinkAndHashtags()
    {
        ResolvedSocialPublicationTarget target = new ResolvedSocialPublicationTarget(
            new Uri("https://amusement-parks.fun/fr/park/park-1/parc-test?facebook-image=image-1#gallery"),
            SocialPublicationTargetKind.Park,
            "Parc Étincelle",
            "Spark Park",
            ImageOwnerType.Park,
            "park-1",
            ImageCategory.Park,
            null);

        string message = SocialPublicationMessageBuilder.BuildDefaultMessage(target);

        Assert.Equal(
            "🌍 Nouveau parc ajouté sur Amusement-Parks.Fun : Parc Étincelle ! 🎢\n\n"
            + "Tu connais ce parc ? Tu l’as déjà visité ? Qu’en as-tu pensé ? Dis-le-nous en commentaire ! 😊\n\n"
            + "🇬🇧 New park added to Amusement-Parks.Fun: Spark Park! 🎢\n\n"
            + "Do you know this park? Have you visited it? What did you think? Tell us in the comments! 😊\n\n"
            + "🔗 Découvre le parc ici / Discover the park here:\n"
            + "https://amusement-parks.fun/fr/park/park-1/parc-test\n\n"
            + "#AmusementParks #ParcAttractions #MontagnesRusses #PassionParcs #Tourisme "
            + "#ThemeParks #RollerCoasters #Travel",
            message);
        Assert.DoesNotContain("facebook-image", message, StringComparison.Ordinal);
        Assert.DoesNotContain("#gallery", message, StringComparison.Ordinal);
    }
}
