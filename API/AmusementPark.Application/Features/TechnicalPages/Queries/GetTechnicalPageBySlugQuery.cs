using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalPages.Results;

namespace AmusementPark.Application.Features.TechnicalPages.Queries;

public sealed record GetTechnicalPageBySlugQuery(string Slug, bool IncludeHidden)
    : IQuery<ApplicationResult<TechnicalPageResult>>;

