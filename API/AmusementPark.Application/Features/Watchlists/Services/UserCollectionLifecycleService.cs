using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class UserCollectionLifecycleService
{
    private readonly IUserCollectionEntryRepository repository;
    private readonly UserCollectionTargetReader targetReader;
    private readonly TimeProvider timeProvider;

    public UserCollectionLifecycleService(
        IUserCollectionEntryRepository repository,
        UserCollectionTargetReader targetReader,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.targetReader = targetReader ?? throw new ArgumentNullException(nameof(targetReader));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<UserCollectionEntryResult>> AddAsync(
        string userId,
        UserCollectionTargetInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            UserCollectionEntry? existing = await this.repository.GetOwnedByIdentityAsync(
                normalizedUserId,
                input.TargetType,
                input.TargetId,
                input.Kind,
                cancellationToken);
            UserCollectionTargetSnapshot snapshot = await this.targetReader.ResolveAsync(
                input.TargetType,
                input.TargetId,
                cancellationToken);
            if (!snapshot.IsAvailableForCreation)
            {
                return ApplicationResult<UserCollectionEntryResult>.Failure(
                    UserCollectionApplicationErrors.TargetNotFound());
            }

            if (existing is not null)
            {
                return ApplicationResult<UserCollectionEntryResult>.Success(ToResult(existing, snapshot));
            }

            UserCollectionEntry entry = UserCollectionEntry.Create(
                UserCollectionEntryId.New(),
                normalizedUserId,
                input.TargetType,
                input.TargetId,
                input.Kind,
                snapshot.Status,
                null,
                null,
                null,
                this.timeProvider.GetUtcNow().UtcDateTime);
            UserCollectionWriteOutcome outcome = await this.repository.CreateAsync(
                entry,
                cancellationToken);
            if (outcome == UserCollectionWriteOutcome.LimitReached)
            {
                return ApplicationResult<UserCollectionEntryResult>.Failure(
                    UserCollectionApplicationErrors.LimitReached());
            }

            if (outcome == UserCollectionWriteOutcome.AlreadyExists)
            {
                UserCollectionEntry? concurrentEntry = await this.repository.GetOwnedByIdentityAsync(
                    normalizedUserId,
                    input.TargetType,
                    input.TargetId,
                    input.Kind,
                    cancellationToken);
                if (concurrentEntry is not null)
                {
                    return ApplicationResult<UserCollectionEntryResult>.Success(
                        ToResult(concurrentEntry, snapshot));
                }

                return ApplicationResult<UserCollectionEntryResult>.Failure(
                    UserCollectionApplicationErrors.ChangedConcurrently());
            }

            return outcome == UserCollectionWriteOutcome.Success
                ? ApplicationResult<UserCollectionEntryResult>.Success(ToResult(entry, snapshot))
                : ApplicationResult<UserCollectionEntryResult>.Failure(
                    UserCollectionApplicationErrors.ChangedConcurrently());
        }
        catch (UserCollectionValidationException exception)
        {
            return Invalid<UserCollectionEntryResult>(exception);
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<UserCollectionEntryResult>.Failure(
                UserCollectionApplicationErrors.Invalid(
                    UserCollectionErrorCodes.InvalidState,
                    exception.Message));
        }
    }

    public async Task<ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>> ListAsync(
        string userId,
        CollectionTargetType? targetType,
        string? targetId,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            IReadOnlyCollection<UserCollectionEntry> entries = await this.repository.ListOwnedAsync(
                normalizedUserId,
                targetType,
                targetId,
                cancellationToken);
            Dictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>>
                snapshotsByType = new();
            foreach (IGrouping<CollectionTargetType, UserCollectionEntry> group in entries.GroupBy(
                static entry => entry.TargetType))
            {
                snapshotsByType[group.Key] = await this.targetReader.ResolveAsync(
                    group.Key,
                    group.Select(static entry => entry.TargetId).ToArray(),
                    cancellationToken);
            }

            UserCollectionEntryResult[] results = entries.Select(entry =>
            {
                UserCollectionTargetSnapshot snapshot = snapshotsByType.TryGetValue(
                    entry.TargetType,
                    out IReadOnlyDictionary<string, UserCollectionTargetSnapshot>? snapshots)
                    && snapshots.TryGetValue(entry.TargetId, out UserCollectionTargetSnapshot? resolved)
                        ? resolved
                        : new UserCollectionTargetSnapshot(
                            entry.TargetType,
                            entry.TargetId,
                            CollectionTargetStatus.Unknown,
                            null,
                            null,
                            null,
                            null);
                return ToResult(entry, snapshot);
            }).ToArray();
            return ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>.Success(results);
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>.Failure(
                UserCollectionApplicationErrors.Invalid(
                    UserCollectionErrorCodes.InvalidState,
                    exception.Message));
        }
    }

    public async Task<ApplicationResult> DeleteAsync(
        string userId,
        UserCollectionTargetInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            await this.repository.DeleteOwnedByIdentityAsync(
                normalizedUserId,
                input.TargetType,
                input.TargetId,
                input.Kind,
                cancellationToken);
            return ApplicationResult.Success();
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult.Failure(
                UserCollectionApplicationErrors.Invalid(
                    UserCollectionErrorCodes.InvalidState,
                    exception.Message));
        }
    }

    private static UserCollectionEntryResult ToResult(
        UserCollectionEntry entry,
        UserCollectionTargetSnapshot snapshot)
    {
        return new UserCollectionEntryResult(
            entry.Id.Value,
            entry.TargetType,
            entry.TargetId,
            entry.Kind,
            snapshot.IsAvailableForCreation ? snapshot.Status : entry.TargetStatus,
            snapshot.Name,
            snapshot.ParentParkId,
            snapshot.ParentParkName,
            snapshot.MainImageId,
            entry.PrivateNote,
            entry.Priority,
            entry.PreferredPeriod?.StartsOn,
            entry.PreferredPeriod?.EndsOn,
            entry.CreatedAtUtc,
            entry.UpdatedAtUtc,
            entry.Version);
    }

    private static ApplicationResult<TResult> Invalid<TResult>(
        UserCollectionValidationException exception)
    {
        return ApplicationResult<TResult>.Failure(
            UserCollectionApplicationErrors.Invalid(exception.Code, exception.Message));
    }
}
