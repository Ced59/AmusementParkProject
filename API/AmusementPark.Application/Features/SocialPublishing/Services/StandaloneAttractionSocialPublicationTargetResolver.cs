using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class StandaloneAttractionSocialPublicationTargetResolver
{
    private readonly IStandaloneAttractionRepository standaloneAttractionRepository;

    public StandaloneAttractionSocialPublicationTargetResolver(
        IStandaloneAttractionRepository standaloneAttractionRepository)
    {
        this.standaloneAttractionRepository = standaloneAttractionRepository;
    }

    internal async Task<ResolvedSocialPublicationTarget?> ResolveAsync(
        Uri normalizedUrl,
        IReadOnlyList<string> segments,
        CancellationToken cancellationToken)
    {
        if (segments.Count < 4
            || string.IsNullOrWhiteSpace(segments[2])
            || string.IsNullOrWhiteSpace(segments[3]))
        {
            return null;
        }

        StandaloneAttraction? attraction = await this.standaloneAttractionRepository.GetByIdAsync(
            segments[2],
            false,
            cancellationToken);
        if (attraction is null
            || !attraction.IsPubliclyPublishable()
            || string.IsNullOrWhiteSpace(attraction.Name))
        {
            return null;
        }

        if (segments.Count == 4)
        {
            return BuildTarget(
                normalizedUrl,
                SocialPublicationTargetKind.StandaloneAttraction,
                attraction.Name,
                attraction.Name,
                attraction.Id!);
        }

        bool isHistoryRoute = segments.Count == 5
            && string.Equals(segments[4], "history", StringComparison.OrdinalIgnoreCase);
        bool isHistoryPageRoute = segments.Count == 7
            && string.Equals(segments[4], "history", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[5], "page", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(segments[6], out int page)
            && page > 0;
        if (!isHistoryRoute && !isHistoryPageRoute)
        {
            return null;
        }

        return BuildTarget(
            normalizedUrl,
            SocialPublicationTargetKind.Page,
            $"L’histoire de {attraction.Name}",
            $"The history of {attraction.Name}",
            attraction.Id!);
    }

    private static ResolvedSocialPublicationTarget BuildTarget(
        Uri normalizedUrl,
        SocialPublicationTargetKind kind,
        string frenchName,
        string englishName,
        string attractionId)
    {
        return new ResolvedSocialPublicationTarget(
            normalizedUrl,
            kind,
            frenchName,
            englishName,
            ImageOwnerType.StandaloneAttraction,
            attractionId,
            ImageCategory.StandaloneAttraction,
            null);
    }
}
