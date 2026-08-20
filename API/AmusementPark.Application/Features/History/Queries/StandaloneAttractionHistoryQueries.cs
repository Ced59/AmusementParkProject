using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Results;

namespace AmusementPark.Application.Features.History.Queries;

public sealed record GetStandaloneAttractionHistoryTimelineQuery(
    string StandaloneAttractionId,
    bool IncludeHidden,
    int Page = HistoryTimelinePaging.DefaultPage,
    int PageSize = HistoryTimelinePaging.DefaultPageSize)
    : IQuery<ApplicationResult<StandaloneAttractionHistoryTimelineResult>>;
