using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class NotificationEmailDeliveryScheduler : INotificationEmailDeliveryScheduler
{
    internal static readonly TimeSpan DigestSettlementDelay = TimeSpan.FromMinutes(2);
    private readonly IDurableBackgroundJobRepository jobRepository;
    private readonly INotificationEmailPreferenceRepository preferenceRepository;
    private readonly TimeProvider timeProvider;

    public NotificationEmailDeliveryScheduler(
        IDurableBackgroundJobRepository jobRepository,
        INotificationEmailPreferenceRepository preferenceRepository,
        TimeProvider? timeProvider = null)
    {
        this.jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        this.preferenceRepository = preferenceRepository
            ?? throw new ArgumentNullException(nameof(preferenceRepository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task ScheduleAsync(
        NotificationDigest digest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(digest);
        NotificationEmailPreference? preference = await this.preferenceRepository.GetAsync(
            digest.UserId,
            cancellationToken);
        if (preference?.IsEnabled != true)
        {
            return;
        }

        DateTime deliverAtUtc = digest.PeriodEndUtc.Add(DigestSettlementDelay);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        TimeSpan delay = deliverAtUtc > nowUtc ? deliverAtUtc - nowUtc : TimeSpan.Zero;
        NotificationEmailDeliveryJobPayload payload = new NotificationEmailDeliveryJobPayload(
            digest.Id.Value);
        await this.jobRepository.EnqueueExactAsync(
            new EnqueueExactBackgroundJobRequest(
                NotificationEmailDeliveryJob.Kind,
                $"watch-email:{digest.Id.Value}",
                NotificationEmailDeliveryJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload),
                Delay: delay,
                CorrelationId: digest.Id.Value),
            cancellationToken);
    }
}
