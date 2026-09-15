using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class UserCollectionLifecycleService
{
    private const int MaximumStatusSynchronizationAttempts = 3;

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
                UserCollectionEntry? synchronizedEntry = await this.EnsureTargetStatusSynchronizedAsync(
                    existing,
                    snapshot,
                    cancellationToken);
                return synchronizedEntry is not null
                    ? ApplicationResult<UserCollectionEntryResult>.Success(
                        ToResult(synchronizedEntry, snapshot))
                    : ApplicationResult<UserCollectionEntryResult>.Failure(
                        UserCollectionApplicationErrors.ChangedConcurrently());
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
                    UserCollectionEntry? synchronizedEntry = await this.EnsureTargetStatusSynchronizedAsync(
                        concurrentEntry,
                        snapshot,
                        cancellationToken);
                    return synchronizedEntry is not null
                        ? ApplicationResult<UserCollectionEntryResult>.Success(
                            ToResult(synchronizedEntry, snapshot))
                        : ApplicationResult<UserCollectionEntryResult>.Failure(
                            UserCollectionApplicationErrors.ChangedConcurrently());
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
            List<UserCollectionEntry> entries = (await this.repository.ListOwnedAsync(
                normalizedUserId,
                targetType,
                targetId,
                cancellationToken)).ToList();
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

            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            List<UserCollectionEntry> synchronizedEntries = new();
            foreach (UserCollectionEntry entry in entries)
            {
                if (snapshotsByType.TryGetValue(
                        entry.TargetType,
                        out IReadOnlyDictionary<string, UserCollectionTargetSnapshot>? snapshots)
                    && snapshots.TryGetValue(entry.TargetId, out UserCollectionTargetSnapshot? snapshot)
                    && snapshot.IsAvailableForCreation
                    && snapshot.Status != entry.TargetStatus)
                {
                    entry.SynchronizeTargetStatus(snapshot.Status, nowUtc);
                    synchronizedEntries.Add(entry);
                }
            }

            if (synchronizedEntries.Count > 0)
            {
                bool allStatusesSynchronized = await this.repository.TrySynchronizeTargetStatusesAsync(
                    synchronizedEntries,
                    cancellationToken);
                if (!allStatusesSynchronized)
                {
                    foreach (UserCollectionEntry synchronizedEntry in synchronizedEntries)
                    {
                        UserCollectionEntry? persistedEntry =
                            await this.RetryTargetStatusSynchronizationAsync(
                                synchronizedEntry,
                                cancellationToken);
                        if (persistedEntry is null)
                        {
                            return ApplicationResult<IReadOnlyCollection<UserCollectionEntryResult>>
                                .Failure(UserCollectionApplicationErrors.ChangedConcurrently());
                        }

                        int entryIndex = entries.FindIndex(entry =>
                            entry.Id == synchronizedEntry.Id);
                        if (entryIndex >= 0)
                        {
                            entries[entryIndex] = persistedEntry;
                        }
                    }
                }
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
            entry.TargetStatus,
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

    private async Task<UserCollectionEntry?> EnsureTargetStatusSynchronizedAsync(
        UserCollectionEntry entry,
        UserCollectionTargetSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!snapshot.IsAvailableForCreation || snapshot.Status == entry.TargetStatus)
        {
            return entry;
        }

        UserCollectionEntry currentEntry = entry;
        UserCollectionTargetSnapshot currentSnapshot = snapshot;
        for (int attempt = 0; attempt < MaximumStatusSynchronizationAttempts; attempt++)
        {
            currentEntry.SynchronizeTargetStatus(
                currentSnapshot.Status,
                this.timeProvider.GetUtcNow().UtcDateTime);
            bool synchronized = await this.repository.TrySynchronizeTargetStatusesAsync(
                new[] { currentEntry },
                cancellationToken);
            if (synchronized)
            {
                return currentEntry;
            }

            UserCollectionEntry? persistedEntry = await this.repository.GetOwnedByIdentityAsync(
                currentEntry.UserId,
                currentEntry.TargetType,
                currentEntry.TargetId,
                currentEntry.Kind,
                cancellationToken);
            if (persistedEntry is null)
            {
                return null;
            }

            currentSnapshot = await this.targetReader.ResolveAsync(
                persistedEntry.TargetType,
                persistedEntry.TargetId,
                cancellationToken);
            if (!currentSnapshot.IsAvailableForCreation
                || persistedEntry.TargetStatus == currentSnapshot.Status)
            {
                return persistedEntry;
            }

            currentEntry = persistedEntry;
        }

        return null;
    }

    private async Task<UserCollectionEntry?> RetryTargetStatusSynchronizationAsync(
        UserCollectionEntry attemptedEntry,
        CancellationToken cancellationToken)
    {
        UserCollectionEntry? persistedEntry = await this.repository.GetOwnedByIdentityAsync(
            attemptedEntry.UserId,
            attemptedEntry.TargetType,
            attemptedEntry.TargetId,
            attemptedEntry.Kind,
            cancellationToken);
        if (persistedEntry is null)
        {
            return null;
        }

        UserCollectionTargetSnapshot refreshedSnapshot = await this.targetReader.ResolveAsync(
            persistedEntry.TargetType,
            persistedEntry.TargetId,
            cancellationToken);
        return !refreshedSnapshot.IsAvailableForCreation
            ? persistedEntry
            : await this.EnsureTargetStatusSynchronizedAsync(
                persistedEntry,
                refreshedSnapshot,
                cancellationToken);
    }

    private static ApplicationResult<TResult> Invalid<TResult>(
        UserCollectionValidationException exception)
    {
        return ApplicationResult<TResult>.Failure(
            UserCollectionApplicationErrors.Invalid(exception.Code, exception.Message));
    }
}
