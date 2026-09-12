using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalPages.Results;
using AmusementPark.Core.Domain.TechnicalPages;

namespace AmusementPark.Application.Features.TechnicalPages.Commands;

public sealed record UpsertTechnicalPagesJsonCommand(IReadOnlyCollection<TechnicalPage> Pages)
    : ICommand<ApplicationResult<TechnicalPageJsonUpsertResult>>;

