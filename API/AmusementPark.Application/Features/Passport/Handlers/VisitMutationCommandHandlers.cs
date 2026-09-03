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
            clock,
            timeZoneValidator,
            auditPublisher)
    {
    }

    public UpdateVisitMetadataCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock,
        IPassportTimeZoneValidator timeZoneValidator,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
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
        if (this.occurrenceRepository is not null
            && TemporalIdentityChanged(
                visit,
                date,
                timeZoneId,
                command.ServiceDayConvention))
        {
            RideOccurrencePage firstOccurrencePage =
                await this.occurrenceRepository.ListOwnedByVisitAsync(
                    new RideOccurrenceListCriteria(
                        visit.Id,
                        visit.UserId,
                        1),
                    cancellationToken);
            if (firstOccurrencePage.Items.Count > 0)
            {
                return ApplicationResult<VisitResult>.Failure(
                    PassportApplicationErrors.VisitTemporalMetadataLocked());
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
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.InvalidVisit(exception.ErrorCode, exception.Message));
        }
        if (visit.Version == command.ExpectedVersion)
        {
            bool versionIsCurrent = await this.visitRepository.TryConfirmOwnedVersionAsync(
                visit.Id,
                visit.UserId,
                command.ExpectedVersion,
                cancellationToken);
            if (!versionIsCurrent)
            {
                return ApplicationResult<VisitResult>.Failure(
                    PassportApplicationErrors.VisitConcurrencyConflict());
            }

            return ApplicationResult<VisitResult>.Success(PassportVisitResultFactory.Create(visit));
        }

        PassportAuditEvent auditEvent = PassportVisitAuditEventFactory.VisitUpdated(visit, previous);
        bool updated = await this.visitRepository.TryUpdateOwnedAuditedAsync(
            visit,
            command.ExpectedVersion,
            auditEvent,
            cancellationToken);
        if (!updated)
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.VisitConcurrencyConflict());
        }

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            cancellationToken);
        return ApplicationResult<VisitResult>.Success(PassportVisitResultFactory.Create(visit));
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

public sealed class CompleteVisitCommandHandler :
    ICommandHandler<CompleteVisitCommand, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportClock clock;
    private readonly IPassportLocalDateResolver localDateResolver;
    private readonly IPassportAuditPublisher auditPublisher;

    public CompleteVisitCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportLocalDateResolver localDateResolver,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.clock = clock;
        this.localDateResolver = localDateResolver;
        this.auditPublisher = auditPublisher;
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
            (visit, nowUtc) => visit.Complete(
                this.localDateResolver.Resolve(nowUtc, visit.TimeZoneId),
                nowUtc),
            cancellationToken);
    }
}

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

public sealed class ArchiveVisitCommandHandler :
    ICommandHandler<ArchiveVisitCommand, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher auditPublisher;

    public ArchiveVisitCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
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
            static (visit, nowUtc) => visit.Archive(nowUtc),
            cancellationToken);
    }
}

internal static class VisitMutationCommandSupport
{
    public static async Task<ApplicationResult<Visit>> LoadAsync(
        string userId,
        string visitIdValue,
        long expectedVersion,
        IUserVisitRepository visitRepository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ApplicationResult<Visit>.Failure(ApplicationErrors.Required(nameof(userId)));
        }

        if (expectedVersion < 1)
        {
            return ApplicationResult<Visit>.Failure(
                PassportApplicationErrors.InvalidVisitVersion());
        }

        VisitId visitId;
        try
        {
            visitId = VisitId.Parse(visitIdValue);
        }
        catch (ArgumentException)
        {
            return ApplicationResult<Visit>.Failure(PassportApplicationErrors.VisitNotFound());
        }

        Visit? visit = await visitRepository.GetOwnedAsync(
            visitId,
            userId.Trim(),
            cancellationToken);
        if (visit is null)
        {
            return ApplicationResult<Visit>.Failure(PassportApplicationErrors.VisitNotFound());
        }

        return visit.Version == expectedVersion
            ? ApplicationResult<Visit>.Success(visit)
            : ApplicationResult<Visit>.Failure(PassportApplicationErrors.VisitConcurrencyConflict());
    }

    public static async Task<ApplicationResult<VisitResult>> ChangeStatusAsync(
        string userId,
        string visitId,
        long expectedVersion,
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher,
        Action<Visit, DateTime> mutation,
        CancellationToken cancellationToken)
    {
        ApplicationResult<Visit> loaded = await LoadAsync(
            userId,
            visitId,
            expectedVersion,
            visitRepository,
            cancellationToken);
        if (!loaded.IsSuccess || loaded.Value is null)
        {
            return ApplicationResult<VisitResult>.Failure(loaded.Errors);
        }

        Visit visit = loaded.Value;
        VisitStatus previousStatus = visit.Status;
        try
        {
            mutation(visit, clock.UtcNow);
        }
        catch (VisitValidationException exception)
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.InvalidVisit(exception.ErrorCode, exception.Message));
        }
        catch (TimeZoneNotFoundException)
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.InvalidTimeZone());
        }
        catch (InvalidTimeZoneException)
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.InvalidTimeZone());
        }

        PassportAuditEvent auditEvent = PassportVisitAuditEventFactory.VisitStatusChanged(
            visit,
            previousStatus);
        bool updated = await visitRepository.TryUpdateOwnedAuditedAsync(
            visit,
            expectedVersion,
            auditEvent,
            cancellationToken);
        if (!updated)
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.VisitConcurrencyConflict());
        }

        await PassportAuditDelivery.PublishAsync(
            auditPublisher,
            auditEvent,
            cancellationToken);
        return ApplicationResult<VisitResult>.Success(PassportVisitResultFactory.Create(visit));
    }
}
