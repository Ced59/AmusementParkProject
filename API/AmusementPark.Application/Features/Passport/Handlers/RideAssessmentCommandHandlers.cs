using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class UpsertRideAssessmentCommandHandler
    : ICommandHandler<UpsertRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;

    public UpsertRideAssessmentCommandHandler(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
    {
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
    }

    public async Task<ApplicationResult<RideOccurrenceResult>> HandleAsync(
        UpsertRideAssessmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ParsedRideAssessmentScope? scope = ParseScope(command.UserId, command.OccurrenceId);
        if (scope is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        if (command.ExpectedVersion < 1)
        {
            return Failure(PassportApplicationErrors.InvalidRideAssessmentVersion());
        }

        RatingValue value;
        try
        {
            value = RatingValue.FromDouble(command.Value);
        }
        catch (RatingValueValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidRideAssessment(
                exception.ErrorCode,
                exception.Message));
        }

        RideOccurrence? occurrence = await this.occurrenceRepository.GetOwnedByIdAsync(
            scope.OccurrenceId,
            scope.UserId,
            cancellationToken);
        if (occurrence is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        if (occurrence.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        long expectedVersion = occurrence.Version;
        try
        {
            occurrence.UpsertAssessment(value, command.PrivateComment, this.clock.UtcNow);
        }
        catch (RideAssessmentValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidRideAssessment(
                exception.ErrorCode,
                exception.Message));
        }
        catch (RideOccurrenceValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidRideOccurrence(
                exception.ErrorCode,
                exception.Message));
        }

        bool updated = await this.occurrenceRepository.TryUpdateOwnedAsync(
            occurrence,
            expectedVersion,
            cancellationToken);
        return updated
            ? Success(occurrence)
            : Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
    }

    private static ParsedRideAssessmentScope? ParseScope(string? userId, string? occurrenceId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        try
        {
            return new ParsedRideAssessmentScope(userId.Trim(), RideOccurrenceId.Parse(occurrenceId));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static ApplicationResult<RideOccurrenceResult> Success(RideOccurrence occurrence)
    {
        return ApplicationResult<RideOccurrenceResult>.Success(
            PassportRideOccurrenceResultFactory.Create(occurrence));
    }

    private static ApplicationResult<RideOccurrenceResult> Failure(ApplicationError error)
    {
        return ApplicationResult<RideOccurrenceResult>.Failure(error);
    }

    private sealed record ParsedRideAssessmentScope(string UserId, RideOccurrenceId OccurrenceId);
}

public sealed class DeleteRideAssessmentCommandHandler
    : ICommandHandler<DeleteRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;

    public DeleteRideAssessmentCommandHandler(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
    {
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
    }

    public async Task<ApplicationResult<RideOccurrenceResult>> HandleAsync(
        DeleteRideAssessmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        if (command.ExpectedVersion < 1)
        {
            return Failure(PassportApplicationErrors.InvalidRideAssessmentVersion());
        }

        RideOccurrenceId occurrenceId;
        try
        {
            occurrenceId = RideOccurrenceId.Parse(command.OccurrenceId);
        }
        catch (ArgumentException)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        string userId = command.UserId.Trim();
        RideOccurrence? occurrence = await this.occurrenceRepository.GetOwnedByIdAsync(
            occurrenceId,
            userId,
            cancellationToken);
        if (occurrence is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        if (occurrence.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        long expectedVersion = occurrence.Version;
        try
        {
            occurrence.DeleteAssessment(this.clock.UtcNow);
        }
        catch (RideOccurrenceValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidRideOccurrence(
                exception.ErrorCode,
                exception.Message));
        }

        if (occurrence.Version == expectedVersion)
        {
            bool versionIsCurrent = await this.occurrenceRepository.TryConfirmOwnedVersionAsync(
                occurrence.Id,
                occurrence.VisitId,
                occurrence.UserId,
                expectedVersion,
                cancellationToken);
            return versionIsCurrent
                ? Success(occurrence)
                : Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        bool updated = await this.occurrenceRepository.TryUpdateOwnedAsync(
            occurrence,
            expectedVersion,
            cancellationToken);
        return updated
            ? Success(occurrence)
            : Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
    }

    private static ApplicationResult<RideOccurrenceResult> Success(RideOccurrence occurrence)
    {
        return ApplicationResult<RideOccurrenceResult>.Success(
            PassportRideOccurrenceResultFactory.Create(occurrence));
    }

    private static ApplicationResult<RideOccurrenceResult> Failure(ApplicationError error)
    {
        return ApplicationResult<RideOccurrenceResult>.Failure(error);
    }
}
