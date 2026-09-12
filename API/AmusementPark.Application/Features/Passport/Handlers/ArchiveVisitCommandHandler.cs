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

public sealed class ArchiveVisitCommandHandler :
    ICommandHandler<ArchiveVisitCommand, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher auditPublisher;
    private readonly IPassportPendingMutationReconciler pendingMutationReconciler;

    public ArchiveVisitCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher,
        IPassportPendingMutationReconciler pendingMutationReconciler)
    {
        this.visitRepository = visitRepository;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
        this.pendingMutationReconciler = pendingMutationReconciler;
    }

    public Task<ApplicationResult<VisitResult>> HandleAsync(
        ArchiveVisitCommand command,
        CancellationToken cancellationToken = default)
    {
        return VisitMutationCommandSupport.ChangeStatusAsync(
            command.UserId,
            command.VisitId,
            command.ExpectedVersion,
            this.visitRepository,
            this.clock,
            this.auditPublisher,
            this.pendingMutationReconciler,
            static (visit, nowUtc) => visit.Archive(nowUtc),
            cancellationToken);
    }
}
