using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Results;

namespace AmusementPark.Application.Features.History.Queries;

public sealed record GetLatestHistoryArticlesQuery(int Limit)
    : IQuery<ApplicationResult<IReadOnlyCollection<HistoryArticleResult>>>;
