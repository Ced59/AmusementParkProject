using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class ReopenVisitCommandHandler :
    ICommandHandler<ReopenVisitCommand, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher auditPublisher;

    public ReopenVisitCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
    }

    public Task<ApplicationResult<VisitResult>> HandleAsync(
        ReopenVisitCommand command,
        CancellationToken cancellationToken = default)
    {
        return VisitMutationCommandSupport.ChangeStatusAsync(
            command.UserId,
            command.VisitId,
            command.ExpectedVersion,
            this.visitRepository,
            this.clock,
            this.auditPublisher,
            null,
            static (visit, nowUtc) =>
            {
                if (visit.Status == VisitStatus.Archived)
                {
                    visit.RestoreAsDraft(nowUtc);
                    return;
                }

                visit.Reopen(nowUtc);
            },
            cancellationToken);
    }
}
