using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.TechnicalPages;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class ContentSocialPublicationTargetResolver
{
    private readonly ITechnicalPageRepository technicalPageRepository;
    private readonly ISharePublicationAccessResolver sharePublicationAccessResolver;

    public ContentSocialPublicationTargetResolver(
        ITechnicalPageRepository technicalPageRepository,
        ISharePublicationAccessResolver sharePublicationAccessResolver)
    {
        this.technicalPageRepository = technicalPageRepository;
        this.sharePublicationAccessResolver = sharePublicationAccessResolver;
    }

    internal async Task<ResolvedSocialPublicationTarget?> ResolveAsync(
        Uri normalizedUrl,
        IReadOnlyList<string> segments,
        CancellationToken cancellationToken)
    {
        if (segments.Count == 3
            && string.Equals(segments[1], "technical", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(segments[2]))
        {
            return await this.ResolveTechnicalPageAsync(normalizedUrl, segments[2], cancellationToken);
        }

        if (segments.Count == 4
            && string.Equals(segments[1], "rankings", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[2], "shared", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(segments[3]))
        {
            return await this.ResolveSharedRankingAsync(normalizedUrl, segments[3], cancellationToken);
        }

        return null;
    }

    private async Task<ResolvedSocialPublicationTarget?> ResolveTechnicalPageAsync(
        Uri normalizedUrl,
        string slug,
        CancellationToken cancellationToken)
    {
        TechnicalPage? page = await this.technicalPageRepository.GetBySlugAsync(slug, false, cancellationToken);
        if (page is null
            || !page.IsVisible
            || page.AdminReviewStatus == AdminReviewStatus.NotRelevant
            || string.IsNullOrWhiteSpace(page.Slug))
        {
            return null;
        }

        string fallback = page.Slug.Replace('-', ' ');
        return BuildTarget(
            normalizedUrl,
            SocialPublicationLocalizedTextResolver.Resolve(page.Titles, "fr", fallback),
            SocialPublicationLocalizedTextResolver.Resolve(page.Titles, "en", fallback));
    }

    private async Task<ResolvedSocialPublicationTarget?> ResolveSharedRankingAsync(
        Uri normalizedUrl,
        string shareId,
        CancellationToken cancellationToken)
    {
        ApplicationResult<ResolvedSharePublicationResult> result =
            await this.sharePublicationAccessResolver.ResolveAsync(
            shareId,
            SharePublicationType.PersonalRanking,
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return null;
        }

        return BuildTarget(
            normalizedUrl,
            "Les classements partagés d’un membre",
            "A member’s shared rankings");
    }

    private static ResolvedSocialPublicationTarget BuildTarget(
        Uri normalizedUrl,
        string frenchName,
        string englishName)
    {
        return new ResolvedSocialPublicationTarget(
            normalizedUrl,
            SocialPublicationTargetKind.Page,
            frenchName,
            englishName,
            null,
            null,
            null,
            null);
    }
}
