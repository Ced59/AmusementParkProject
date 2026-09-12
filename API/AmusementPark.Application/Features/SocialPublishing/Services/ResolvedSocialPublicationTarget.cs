using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

internal sealed record ResolvedSocialPublicationTarget(
    Uri Url,
    SocialPublicationTargetKind Kind,
    string FrenchName,
    string EnglishName,
    ImageOwnerType? ImageOwnerType,
    string? ImageOwnerId,
    ImageCategory? ImageCategory,
    Park? Park)
{
    public string LanguageCode
    {
        get
        {
            return this.Url.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.Trim()
                .ToLowerInvariant()
                ?? "fr";
        }
    }
}
