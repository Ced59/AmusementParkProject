using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Services;

public sealed class ParkFitGroupProfileLifecycleService
{
    private readonly IParkFitGroupProfileRepository repository;
    private readonly TimeProvider timeProvider;

    public ParkFitGroupProfileLifecycleService(
        IParkFitGroupProfileRepository repository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ParkFitGroupProfileResult>> CreateAsync(
        string ownerUserId,
        ParkFitGroupProfileInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        try
        {
            string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
                ownerUserId,
                nameof(ownerUserId));
            long ownedProfileCount = await this.repository.CountOwnedAsync(
                normalizedOwnerUserId,
                cancellationToken);
            if (ownedProfileCount >= ParkFitGroupProfile.MaximumProfilesPerOwner)
            {
                return ApplicationResult<ParkFitGroupProfileResult>.Failure(
                    ParkFitGroupProfileApplicationErrors.ProfileLimitReached());
            }

            ParkFitGroupProfile profile = ParkFitGroupProfile.Create(
                ParkFitGroupProfileId.New(),
                normalizedOwnerUserId,
                input.Alias,
                input.HeightCentimeters,
                input.AgeYears,
                input.CanBeAccompanied,
                input.CompanionAgeYears,
                this.timeProvider.GetUtcNow().UtcDateTime);
            ParkFitGroupProfileWriteOutcome outcome = await this.repository.CreateAsync(
                profile,
                cancellationToken);
            return outcome == ParkFitGroupProfileWriteOutcome.Success
                ? ApplicationResult<ParkFitGroupProfileResult>.Success(profile.ToResult())
                : ApplicationResult<ParkFitGroupProfileResult>.Failure(
                    ParkFitGroupProfileApplicationErrors.AliasConflict());
        }
        catch (ParkFitGroupProfileValidationException exception)
        {
            return Invalid<ParkFitGroupProfileResult>(exception);
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<ParkFitGroupProfileResult>.Failure(
                ParkFitGroupProfileApplicationErrors.Invalid(
                    ParkFitGroupProfileErrorCodes.InvalidState,
                    exception.Message));
        }
    }

    public async Task<ApplicationResult<ParkFitGroupProfileResult>> UpdateAsync(
        string ownerUserId,
        string profileId,
        long expectedVersion,
        ParkFitGroupProfileInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!ParkFitGroupProfileId.TryParse(profileId, out ParkFitGroupProfileId id)
            || string.IsNullOrWhiteSpace(ownerUserId))
        {
            return ApplicationResult<ParkFitGroupProfileResult>.Failure(
                ParkFitGroupProfileApplicationErrors.NotFound());
        }

        ParkFitGroupProfile? profile = await this.repository.GetOwnedAsync(
            id,
            ownerUserId.Trim(),
            cancellationToken);
        if (profile is null)
        {
            return ApplicationResult<ParkFitGroupProfileResult>.Failure(
                ParkFitGroupProfileApplicationErrors.NotFound());
        }

        if (expectedVersion != profile.Version)
        {
            return ApplicationResult<ParkFitGroupProfileResult>.Failure(
                ParkFitGroupProfileApplicationErrors.ChangedConcurrently());
        }

        try
        {
            profile.Update(
                input.Alias,
                input.HeightCentimeters,
                input.AgeYears,
                input.CanBeAccompanied,
                input.CompanionAgeYears,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (ParkFitGroupProfileValidationException exception)
        {
            return Invalid<ParkFitGroupProfileResult>(exception);
        }

        ParkFitGroupProfileWriteOutcome outcome = await this.repository.ReplaceAsync(
            profile,
            expectedVersion,
            cancellationToken);
        return outcome switch
        {
            ParkFitGroupProfileWriteOutcome.Success =>
                ApplicationResult<ParkFitGroupProfileResult>.Success(profile.ToResult()),
            ParkFitGroupProfileWriteOutcome.AliasConflict =>
                ApplicationResult<ParkFitGroupProfileResult>.Failure(
                    ParkFitGroupProfileApplicationErrors.AliasConflict()),
            _ => ApplicationResult<ParkFitGroupProfileResult>.Failure(
                ParkFitGroupProfileApplicationErrors.ChangedConcurrently()),
        };
    }

    public async Task<ApplicationResult> DeleteAsync(
        string ownerUserId,
        string profileId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (!ParkFitGroupProfileId.TryParse(profileId, out ParkFitGroupProfileId id)
            || string.IsNullOrWhiteSpace(ownerUserId))
        {
            return ApplicationResult.Failure(ParkFitGroupProfileApplicationErrors.NotFound());
        }

        ParkFitGroupProfile? profile = await this.repository.GetOwnedAsync(
            id,
            ownerUserId.Trim(),
            cancellationToken);
        if (profile is null)
        {
            return ApplicationResult.Failure(ParkFitGroupProfileApplicationErrors.NotFound());
        }

        if (expectedVersion != profile.Version)
        {
            return ApplicationResult.Failure(
                ParkFitGroupProfileApplicationErrors.ChangedConcurrently());
        }

        ParkFitGroupProfileWriteOutcome outcome = await this.repository.DeleteOwnedAsync(
            id,
            profile.OwnerUserId,
            expectedVersion,
            cancellationToken);
        return outcome == ParkFitGroupProfileWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(
                ParkFitGroupProfileApplicationErrors.ChangedConcurrently());
    }

    private static ApplicationResult<TResult> Invalid<TResult>(
        ParkFitGroupProfileValidationException exception)
    {
        return ApplicationResult<TResult>.Failure(
            ParkFitGroupProfileApplicationErrors.Invalid(exception.Code, exception.Message));
    }
}
