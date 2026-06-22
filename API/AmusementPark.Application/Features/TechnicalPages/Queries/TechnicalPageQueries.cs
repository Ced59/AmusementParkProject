using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalPages.Results;

namespace AmusementPark.Application.Features.TechnicalPages.Queries;

public sealed record GetTechnicalPagesQuery(bool IncludeHidden)
    : IQuery<ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>>;

public sealed record GetTechnicalPageLinkIndexQuery()
    : IQuery<ApplicationResult<IReadOnlyCollection<TechnicalPageResult>>>;

public sealed record GetTechnicalPageByIdQuery(string Id)
    : IQuery<ApplicationResult<TechnicalPageResult>>;

public sealed record GetTechnicalPageBySlugQuery(string Slug, bool IncludeHidden)
    : IQuery<ApplicationResult<TechnicalPageResult>>;
