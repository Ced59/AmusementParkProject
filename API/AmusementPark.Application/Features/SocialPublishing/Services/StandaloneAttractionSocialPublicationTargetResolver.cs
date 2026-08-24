using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class StandaloneAttractionSocialPublicationTargetResolver
{
    private readonly IStandaloneAttractionRepository standaloneAttractionRepository;
    private readonly IQueryHandler<GetStandaloneAttractionHistoryTimelineQuery, ApplicationResult<StandaloneAttractionHistoryTimelineResult>> historyTimelineQueryHandler;

    public StandaloneAttractionSocialPublicationTargetResolver(
        IStandaloneAttractionRepository standaloneAttractionRepository,
        IQueryHandler<GetStandaloneAttractionHistoryTimelineQuery, ApplicationResult<StandaloneAttractionHistoryTimelineResult>> historyTimelineQueryHandler)
    {
        this.standaloneAttractionRepository = standaloneAttractionRepository;
        this.historyTimelineQueryHandler = historyTimelineQueryHandler;
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
        int requestedPage = 1;
        bool isHistoryPageRoute = segments.Count == 7
            && string.Equals(segments[4], "history", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[5], "page", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(segments[6], out requestedPage)
            && requestedPage > 0;
        if (!isHistoryRoute && !isHistoryPageRoute)
        {
            return null;
        }

        ApplicationResult<StandaloneAttractionHistoryTimelineResult> historyResult = await this.historyTimelineQueryHandler.HandleAsync(
            new GetStandaloneAttractionHistoryTimelineQuery(attraction.Id!, IncludeHidden: false, Page: requestedPage),
            cancellationToken);
        if (!historyResult.IsSuccess || historyResult.Value is null)
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
