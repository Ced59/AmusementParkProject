using AmusementPark.Application.Common.Contracts;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Images;

public sealed partial class MinioImageBinaryStorage
{
    private static readonly TimeSpan CommentDraftUploadLeaseDuration =
        TimeSpan.FromHours(25);
    private static readonly TimeSpan CommentDraftUploadLeaseRenewalInterval =
        TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyCollection<string>>
        SaveCommentDraftWithoutMetadataAsync(
            string pathWithoutExtension,
            FilePayload file,
            bool withWatermark,
            CancellationToken cancellationToken)
    {
        return await this.ExecuteWithCommentDraftUploadLeaseAsync(
            pathWithoutExtension,
            token => this.SaveCoreAsync(
                pathWithoutExtension,
                file,
                withWatermark,
                true,
                token),
            CommentDraftUploadLeaseDuration,
            CommentDraftUploadLeaseRenewalInterval,
            cancellationToken);
    }

    internal async Task<IReadOnlyCollection<string>>
        ExecuteWithCommentDraftUploadLeaseAsync(
            string pathWithoutExtension,
            Func<CancellationToken, Task<IReadOnlyCollection<string>>> action,
            TimeSpan leaseDuration,
            TimeSpan renewalInterval,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathWithoutExtension);
        ArgumentNullException.ThrowIfNull(action);
        if (leaseDuration <= TimeSpan.Zero
            || renewalInterval <= TimeSpan.Zero
            || renewalInterval >= leaseDuration)
        {
            throw new ArgumentOutOfRangeException(
                nameof(renewalInterval),
                "The renewal interval must be positive and shorter than the lease.");
        }

        string leaseToken = Guid.NewGuid().ToString("N");
        DateTime acquiredAtUtc = DateTime.UtcNow;
        bool leaseAcquired = await this.variantGenerationLease.TryAcquireAsync(
            pathWithoutExtension,
            leaseToken,
            acquiredAtUtc,
            acquiredAtUtc.Add(leaseDuration),
            cancellationToken);
        if (!leaseAcquired)
        {
            throw new InvalidOperationException(
                "Unable to acquire the comment draft upload lease.");
        }

        using CancellationTokenSource actionCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using CancellationTokenSource heartbeatCancellation =
            new CancellationTokenSource();
        TaskCompletionSource<bool> leaseLost =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        Task heartbeatTask = this.MaintainCommentDraftUploadLeaseAsync(
            pathWithoutExtension,
            leaseToken,
            leaseDuration,
            renewalInterval,
            leaseLost,
            heartbeatCancellation.Token);
        bool uploadSucceeded = false;
        try
        {
            Task<IReadOnlyCollection<string>> uploadTask =
                action(actionCancellation.Token);
            Task completedTask = await Task.WhenAny(
                uploadTask,
                leaseLost.Task);
            if (completedTask == leaseLost.Task)
            {
                actionCancellation.Cancel();
                Exception? uploadException = null;
                try
                {
                    _ = await uploadTask;
                }
                catch (Exception exception)
                {
                    uploadException = exception;
                }

                throw new InvalidOperationException(
                    "The comment draft upload lease was lost.",
                    uploadException);
            }

            IReadOnlyCollection<string> result = await uploadTask;
            if (leaseLost.Task.IsCompleted)
            {
                throw new InvalidOperationException(
                    "The comment draft upload lease was lost.");
            }

            uploadSucceeded = true;
            return result;
        }
        finally
        {
            heartbeatCancellation.Cancel();
            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
                when (heartbeatCancellation.IsCancellationRequested)
            {
            }

            if (uploadSucceeded)
            {
                try
                {
                    await this.variantGenerationLease.ReleaseAsync(
                        pathWithoutExtension,
                        leaseToken,
                        CancellationToken.None);
                }
                catch (Exception exception)
                {
                    this.logger.LogWarning(
                        exception,
                        "Unable to release comment draft upload lease for {PathWithoutExtension}.",
                        pathWithoutExtension);
                }
            }
        }
    }

    private async Task MaintainCommentDraftUploadLeaseAsync(
        string pathWithoutExtension,
        string leaseToken,
        TimeSpan leaseDuration,
        TimeSpan renewalInterval,
        TaskCompletionSource<bool> leaseLost,
        CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await Task.Delay(renewalInterval, cancellationToken);
                DateTime renewedAtUtc = DateTime.UtcNow;
                bool renewed = await this.variantGenerationLease.RenewAsync(
                    pathWithoutExtension,
                    leaseToken,
                    renewedAtUtc,
                    renewedAtUtc.Add(leaseDuration),
                    cancellationToken);
                if (!renewed)
                {
                    leaseLost.TrySetResult(true);
                    return;
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Unable to renew comment draft upload lease for {PathWithoutExtension}.",
                pathWithoutExtension);
            leaseLost.TrySetResult(true);
        }
    }
}
