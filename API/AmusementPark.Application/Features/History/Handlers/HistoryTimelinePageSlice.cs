using AmusementPark.Application.Features.History.Results;

namespace AmusementPark.Application.Features.History.Handlers;

internal sealed class HistoryTimelinePageSlice
{
    public IReadOnlyCollection<HistoryTimelineEventResult> Events { get; init; } = Array.Empty<HistoryTimelineEventResult>();

    public HistoryTimelinePaginationResult Pagination { get; init; } = new HistoryTimelinePaginationResult();

    public IReadOnlyCollection<HistoryTimelinePageRangeResult> PageRanges { get; init; } = Array.Empty<HistoryTimelinePageRangeResult>();

    public static HistoryTimelinePageSlice? Create(IReadOnlyCollection<HistoryTimelineEventResult> events, int page, int pageSize)
    {
        if (events.Count == 0)
        {
            return null;
        }

        int safePage = Math.Max(HistoryTimelinePaging.DefaultPage, page);
        int safePageSize = Math.Clamp(pageSize, 1, 100);
        int totalItems = events.Count;
        int totalPages = (int)Math.Ceiling(totalItems / (double)safePageSize);

        if (safePage > totalPages)
        {
            return null;
        }

        List<HistoryTimelineEventResult> orderedEvents = events.ToList();
        List<HistoryTimelinePageRangeResult> pageRanges = new List<HistoryTimelinePageRangeResult>();
        int pageNumber = 1;

        for (int skip = 0; skip < orderedEvents.Count; skip += safePageSize)
        {
            List<HistoryTimelineEventResult> rangeEvents = orderedEvents
                .Skip(skip)
                .Take(safePageSize)
                .ToList();

            if (rangeEvents.Count == 0)
            {
                continue;
            }

            pageRanges.Add(new HistoryTimelinePageRangeResult
            {
                Page = pageNumber,
                StartYear = rangeEvents.First().Event.Year,
                EndYear = rangeEvents.Last().Event.Year,
                EventCount = rangeEvents.Count,
            });
            pageNumber++;
        }

        List<HistoryTimelineEventResult> currentPageEvents = orderedEvents
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new HistoryTimelinePageSlice
        {
            Events = currentPageEvents,
            Pagination = new HistoryTimelinePaginationResult
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = safePage,
                ItemsPerPage = safePageSize,
            },
            PageRanges = pageRanges,
        };
    }
}
