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

public sealed class CompleteVisitCommandHandler :
    ICommandHandler<CompleteVisitCommand, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportClock clock;
    private readonly IPassportLocalDateResolver localDateResolver;
    private readonly IPassportAuditPublisher auditPublisher;
    private readonly IPassportPendingMutationReconciler pendingMutationReconciler;

    public CompleteVisitCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportLocalDateResolver localDateResolver,
        IPassportAuditPublisher auditPublisher,
        IPassportPendingMutationReconciler pendingMutationReconciler)
    {
        this.visitRepository = visitRepository;
        this.clock = clock;
        this.localDateResolver = localDateResolver;
        this.auditPublisher = auditPublisher;
        this.pendingMutationReconciler = pendingMutationReconciler;
    }

    public Task<ApplicationResult<VisitResult>> HandleAsync(
        CompleteVisitCommand command,
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
            (visit, nowUtc) => visit.Complete(
                this.localDateResolver.Resolve(nowUtc, visit.TimeZoneId),
                nowUtc),
            cancellationToken);
    }
}
