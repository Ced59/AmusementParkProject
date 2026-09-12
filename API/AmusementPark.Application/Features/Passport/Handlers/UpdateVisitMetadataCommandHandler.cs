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

public sealed class UpdateVisitMetadataCommandHandler :
    ICommandHandler<UpdateVisitMetadataCommand, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository? occurrenceRepository;
    private readonly IVisitContentMutationLeaseManager? contentMutationLeaseManager;
    private readonly IPassportClock clock;
    private readonly IPassportTimeZoneValidator timeZoneValidator;
    private readonly IPassportAuditPublisher auditPublisher;

    internal UpdateVisitMetadataCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportTimeZoneValidator timeZoneValidator,
        IPassportAuditPublisher auditPublisher)
        : this(
            visitRepository,
            null!,
            null!,
            clock,
            timeZoneValidator,
            auditPublisher)
    {
    }

    public UpdateVisitMetadataCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitContentMutationLeaseManager contentMutationLeaseManager,
        IPassportClock clock,
        IPassportTimeZoneValidator timeZoneValidator,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.contentMutationLeaseManager = contentMutationLeaseManager;
        this.clock = clock;
        this.timeZoneValidator = timeZoneValidator;
        this.auditPublisher = auditPublisher;
    }

    public async Task<ApplicationResult<VisitResult>> HandleAsync(
        UpdateVisitMetadataCommand command,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<Visit> loaded = await VisitMutationCommandSupport.LoadAsync(
            command.UserId,
            command.VisitId,
            command.ExpectedVersion,
            this.visitRepository,
            cancellationToken);
        if (!loaded.IsSuccess || loaded.Value is null)
        {
            return ApplicationResult<VisitResult>.Failure(loaded.Errors);
        }

        VisitDate date;
        try
        {
            date = new VisitDate(
                command.Year,
                command.Month,
                command.Day,
                command.Precision,
                command.IsApproximate);
        }
        catch (VisitDateValidationException exception)
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.InvalidDate(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        string? timeZoneId = string.IsNullOrWhiteSpace(command.TimeZoneId)
            ? null
            : command.TimeZoneId.Trim();
        if (timeZoneId is not null && !this.timeZoneValidator.IsValid(timeZoneId))
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.InvalidTimeZone());
        }

        Visit visit = loaded.Value;
        bool temporalIdentityChanged = TemporalIdentityChanged(
            visit,
            date,
            timeZoneId,
            command.ServiceDayConvention);
        IVisitContentMutationLease? contentMutationLease = null;
        if (temporalIdentityChanged
            && this.occurrenceRepository is not null
            && this.contentMutationLeaseManager is not null)
        {
            contentMutationLease = await this.contentMutationLeaseManager.TryAcquireAsync(
                visit,
                this.clock.UtcNow,
                cancellationToken);
            if (contentMutationLease is null)
            {
                return ApplicationResult<VisitResult>.Failure(
                    PassportApplicationErrors.VisitConcurrencyConflict());
            }
        }

        await using IVisitContentMutationLease? contentMutationLeaseScope =
            contentMutationLease;
        using CancellationTokenSource? leaseCancellationSource =
            PassportLeaseCancellation.Link(contentMutationLease, cancellationToken);
        CancellationToken guardedCancellationToken =
            leaseCancellationSource?.Token ?? cancellationToken;
        if (temporalIdentityChanged && this.occurrenceRepository is not null)
        {
            RideOccurrencePage firstOccurrencePage =
                await this.occurrenceRepository.ListOwnedByVisitAsync(
                    new RideOccurrenceListCriteria(
                        visit.Id,
                        visit.UserId,
                        1),
                    guardedCancellationToken);
            if (firstOccurrencePage.Items.Count > 0)
            {
                return PassportContentMutationLeaseCompletion.Complete(
                    contentMutationLease,
                    ApplicationResult<VisitResult>.Failure(
                        PassportApplicationErrors.VisitTemporalMetadataLocked()));
            }
        }

        VisitAuditSnapshot previous = VisitAuditSnapshot.Capture(visit);
        try
        {
            visit.UpdateDraft(
                date,
                timeZoneId,
                command.ServiceDayConvention,
                command.Title,
                command.PrivateNote,
                this.clock.UtcNow);
        }
        catch (VisitValidationException exception)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ApplicationResult<VisitResult>.Failure(
                    PassportApplicationErrors.InvalidVisit(
                        exception.ErrorCode,
                        exception.Message)));
        }
        if (visit.Version == command.ExpectedVersion)
        {
            bool versionIsCurrent = await this.visitRepository.TryConfirmOwnedVersionAsync(
                visit.Id,
                visit.UserId,
                command.ExpectedVersion,
                guardedCancellationToken);
            if (!versionIsCurrent)
            {
                return PassportContentMutationLeaseCompletion.Complete(
                    contentMutationLease,
                    ApplicationResult<VisitResult>.Failure(
                        PassportApplicationErrors.VisitConcurrencyConflict()));
            }

            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ApplicationResult<VisitResult>.Success(
                    PassportVisitResultFactory.Create(visit)));
        }

        PassportAuditEvent auditEvent = PassportVisitAuditEventFactory.VisitUpdated(visit, previous);
        bool updated = contentMutationLease is null
            ? await this.visitRepository.TryUpdateOwnedAuditedAsync(
                visit,
                command.ExpectedVersion,
                auditEvent,
                guardedCancellationToken)
            : await this.visitRepository.TryUpdateOwnedAuditedWithinContentMutationLeaseAsync(
                visit,
                command.ExpectedVersion,
                auditEvent,
                contentMutationLease.Token,
                guardedCancellationToken);
        if (!updated)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ApplicationResult<VisitResult>.Failure(
                    PassportApplicationErrors.VisitConcurrencyConflict()));
        }

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            guardedCancellationToken);
        return PassportContentMutationLeaseCompletion.Complete(
            contentMutationLease,
            ApplicationResult<VisitResult>.Success(
                PassportVisitResultFactory.Create(visit)));
    }

    private static bool TemporalIdentityChanged(
        Visit visit,
        VisitDate date,
        string? timeZoneId,
        LocalServiceDayConvention serviceDayConvention)
    {
        return visit.Date.Year != date.Year
            || visit.Date.Month != date.Month
            || visit.Date.Day != date.Day
            || visit.Date.Precision != date.Precision
            || !string.Equals(visit.TimeZoneId, timeZoneId, StringComparison.Ordinal)
            || visit.ServiceDayConvention != serviceDayConvention;
    }
}
