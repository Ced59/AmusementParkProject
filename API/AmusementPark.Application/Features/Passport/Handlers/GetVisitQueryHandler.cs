using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetVisitQueryHandler : IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;

    public GetVisitQueryHandler(IUserVisitRepository visitRepository)
    {
        this.visitRepository = visitRepository;
    }

    public async Task<ApplicationResult<VisitResult>> HandleAsync(
        GetVisitQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<VisitResult>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        VisitId visitId;
        try
        {
            visitId = VisitId.Parse(query.VisitId);
        }
        catch (ArgumentException)
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            query.UserId.Trim(),
            cancellationToken);
        return visit is null
            ? ApplicationResult<VisitResult>.Failure(PassportApplicationErrors.VisitNotFound())
            : ApplicationResult<VisitResult>.Success(PassportVisitResultFactory.Create(visit));
    }
}
