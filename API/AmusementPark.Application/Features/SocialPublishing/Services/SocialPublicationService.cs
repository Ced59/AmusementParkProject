using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class SocialPublicationService : ISocialPublicationService
{
    internal const int MaximumMessageLength = 5000;

    private readonly ISocialPublicationRepository repository;
    private readonly IReadOnlyCollection<ISocialPublisher> publishers;
    private readonly IPublicSeoContextProvider publicSeoContextProvider;
    private readonly ISsrPageCacheInvalidator ssrPageCacheInvalidator;

    public SocialPublicationService(
        ISocialPublicationRepository repository,
        IEnumerable<ISocialPublisher> publishers,
        IPublicSeoContextProvider publicSeoContextProvider,
        ISsrPageCacheInvalidator ssrPageCacheInvalidator)
    {
        this.repository = repository;
        this.publishers = publishers.ToList();
        this.publicSeoContextProvider = publicSeoContextProvider;
        this.ssrPageCacheInvalidator = ssrPageCacheInvalidator;
    }

    public async Task<ApplicationResult<SocialPublication>> PublishManualAsync(
        SocialLinkPublicationRequest request,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        return await this.PublishNewAsync(
            request.Network,
            request.Message,
            request.Url,
            SocialPublicationTrigger.Manual,
            null,
            null,
            requestedByUserId,
            null,
            cancellationToken);
    }

    public async Task<ApplicationResult<SocialPublication>> RetryAsync(
        string publicationId,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        SocialPublication? publication = await this.repository.GetByIdAsync(publicationId, cancellationToken);
        if (publication is null)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.PublicationNotFound(publicationId));
        }

        if (publication.Status != SocialPublicationStatus.Failed)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.PublicationCannotBeRetried());
        }

        publication.RequestedByUserId = requestedByUserId;
        publication.Status = SocialPublicationStatus.Pending;
        publication.FailureCode = null;
        publication.FailureMessage = null;
        publication.Touch();
        publication = await this.repository.UpdateAsync(publication, cancellationToken);
        publication = await this.AttemptPublicationAsync(publication, cancellationToken);
        return ApplicationResult<SocialPublication>.Success(publication);
    }

    public async Task<ApplicationResult<SocialPublication>> UpdateAsync(
        string publicationId,
        string? message,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        SocialPublication? publication = await this.repository.GetByIdAsync(publicationId, cancellationToken);
        if (publication is null)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.PublicationNotFound(publicationId));
        }

        if (!CanManage(publication))
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.PublicationCannotBeManaged());
        }

        string normalizedMessage = message?.Trim() ?? string.Empty;
        if (normalizedMessage.Length == 0 || normalizedMessage.Length > MaximumMessageLength)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.InvalidMessage());
        }

        ISocialPublisher? publisher = this.FindPublisher(publication.Network);
        if (publisher is null || !publisher.Describe().IsConfigured)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.PublisherOperationFailed(null));
        }

        SocialPublisherOperationResult result;
        try
        {
            result = await publisher.UpdatePostAsync(publication.ExternalPostId!, normalizedMessage, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            result = new SocialPublisherOperationResult(false, false, "publisher-unavailable", null);
        }

        if (result.IsMissing)
        {
            publication = await this.MarkDeletedAsync(publication, requestedByUserId, cancellationToken);
            return ApplicationResult<SocialPublication>.Success(publication);
        }

        if (!result.IsSuccess)
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublisherOperationFailed(result.FailureMessage));
        }

        publication.Message = normalizedMessage;
        publication.RequestedByUserId = requestedByUserId;
        publication.LastSynchronizedAtUtc = DateTime.UtcNow;
        publication.Touch();
        publication = await this.repository.UpdateAsync(publication, cancellationToken);
        return ApplicationResult<SocialPublication>.Success(publication);
    }

    public async Task<ApplicationResult<SocialPublication>> DeleteAsync(
        string publicationId,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        SocialPublication? publication = await this.repository.GetByIdAsync(publicationId, cancellationToken);
        if (publication is null)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.PublicationNotFound(publicationId));
        }

        if (!CanManage(publication))
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.PublicationCannotBeManaged());
        }

        ISocialPublisher? publisher = this.FindPublisher(publication.Network);
        if (publisher is null || !publisher.Describe().IsConfigured)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.PublisherOperationFailed(null));
        }

        SocialPublisherOperationResult result;
        try
        {
            result = await publisher.DeletePostAsync(publication.ExternalPostId!, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            result = new SocialPublisherOperationResult(false, false, "publisher-unavailable", null);
        }

        if (!result.IsSuccess && !result.IsMissing)
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublisherOperationFailed(result.FailureMessage));
        }

        publication = await this.MarkDeletedAsync(publication, requestedByUserId, cancellationToken);
        return ApplicationResult<SocialPublication>.Success(publication);
    }

    public async Task<SocialPublicationSynchronizationResult> SynchronizeAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<SocialPublication> publications = await this.repository.ListRecentAsync(
            Math.Clamp(limit, 1, 100),
            cancellationToken);
        int checkedCount = 0;
        int updatedCount = 0;
        int deletedCount = 0;
        int failureCount = 0;

        foreach (SocialPublication publication in publications.Where(CanManage))
        {
            ISocialPublisher? publisher = this.FindPublisher(publication.Network);
            if (publisher is null || !publisher.Describe().IsConfigured)
            {
                failureCount++;
                continue;
            }

            checkedCount++;
            SocialPublisherPostSnapshotResult snapshot;
            try
            {
                snapshot = await publisher.GetPostAsync(publication.ExternalPostId!, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                failureCount++;
                continue;
            }

            if (!snapshot.IsSuccess)
            {
                failureCount++;
                continue;
            }

            if (!snapshot.Exists)
            {
                await this.MarkDeletedAsync(publication, publication.RequestedByUserId, cancellationToken);
                deletedCount++;
                continue;
            }

            string? synchronizedMessage = NormalizeExternalMessage(snapshot.Message);
            bool changed = synchronizedMessage is not null
                && !string.Equals(publication.Message, synchronizedMessage, StringComparison.Ordinal);
            publication.Message = synchronizedMessage ?? publication.Message;
            publication.ExternalPostUrl = snapshot.ExternalPostUrl ?? publication.ExternalPostUrl;
            publication.LastSynchronizedAtUtc = DateTime.UtcNow;
            publication.Touch();
            await this.repository.UpdateAsync(publication, cancellationToken);
            if (changed)
            {
                updatedCount++;
            }
        }

        return new SocialPublicationSynchronizationResult(checkedCount, updatedCount, deletedCount, failureCount);
    }

    public async Task ApplyExternalChangeAsync(
        SocialNetwork network,
        SocialWebhookChange change,
        CancellationToken cancellationToken)
    {
        SocialPublication? publication = await this.repository.GetByExternalPostIdAsync(change.ExternalPostId, cancellationToken);
        if (publication is null || publication.Network != network || publication.Status == SocialPublicationStatus.Deleted)
        {
            return;
        }

        if (change.Kind == SocialWebhookChangeKind.Deleted)
        {
            await this.MarkDeletedAsync(publication, publication.RequestedByUserId, cancellationToken);
            return;
        }

        publication.Message = NormalizeExternalMessage(change.Message) ?? publication.Message;
        publication.LastSynchronizedAtUtc = DateTime.UtcNow;
        publication.Touch();
        await this.repository.UpdateAsync(publication, cancellationToken);
    }

    public async Task<SocialPublication?> PublishParkAnnouncementAsync(
        Park park,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(park);

        if (!park.IsPubliclyDiscoverable())
        {
            return null;
        }

        string deduplicationKey = $"facebook:park:{park.Id}";
        SocialPublication? existingPublication = await this.repository.GetByDeduplicationKeyAsync(deduplicationKey, cancellationToken);
        if (existingPublication is not null)
        {
            return existingPublication;
        }

        PublicSeoContext context = await this.publicSeoContextProvider.GetAsync(cancellationToken);
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string parkPath = $"/fr/park/{Uri.EscapeDataString(park.Id!)}/{parkSlug}";
        string url = $"{context.PublicBaseUrl.TrimEnd('/')}{parkPath}";
        string message = SocialPublicationMessageBuilder.BuildParkAnnouncementMessage(
            park.Name!,
            park.Name!,
            new Uri(url, UriKind.Absolute));

        await this.PreservePublicPageDuringSocialPreparationAsync(parkPath, cancellationToken);

        ApplicationResult<SocialPublication> result = await this.PublishNewAsync(
            SocialNetwork.Facebook,
            message,
            url,
            SocialPublicationTrigger.AutomaticParkPublication,
            "Park",
            park.Id,
            requestedByUserId,
            deduplicationKey,
            cancellationToken);

        return result.Value;
    }

    public async Task<ApplicationResult<SocialPublication>> RefreshParkAnnouncementPreviewAsync(
        string parkId,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        SocialPublication? publication = normalizedParkId.Length == 0
            ? null
            : await this.repository.GetByDeduplicationKeyAsync(
                $"facebook:park:{normalizedParkId}",
                cancellationToken);
        if (publication is null)
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublicationNotFound(normalizedParkId));
        }

        if (publication.Network != SocialNetwork.Facebook
            || publication.Trigger != SocialPublicationTrigger.AutomaticParkPublication
            || !CanManage(publication)
            || !Uri.TryCreate(publication.Url, UriKind.Absolute, out Uri? publicationUri))
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublicationCannotBeManaged());
        }

        ISocialPublisher? publisher = this.FindPublisher(publication.Network);
        if (publisher is null || !publisher.Describe().IsConfigured)
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublisherOperationFailed(null));
        }

        await this.PreservePublicPageDuringSocialPreparationAsync(publicationUri.AbsolutePath, cancellationToken);

        SocialPublisherOperationResult refreshResult;
        try
        {
            refreshResult = await publisher.RefreshLinkPreviewAsync(publication.Url, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            refreshResult = new SocialPublisherOperationResult(false, false, "publisher-unavailable", null);
        }

        if (!refreshResult.IsSuccess)
        {
            return ApplicationResult<SocialPublication>.Failure(
                SocialPublishingApplicationErrors.PublisherOperationFailed(refreshResult.FailureMessage));
        }

        publication.RequestedByUserId = requestedByUserId;
        publication.Touch();
        publication = await this.repository.UpdateAsync(publication, cancellationToken);
        return ApplicationResult<SocialPublication>.Success(publication);
    }

    private async Task<ApplicationResult<SocialPublication>> PublishNewAsync(
        SocialNetwork network,
        string? message,
        string? url,
        SocialPublicationTrigger trigger,
        string? sourceEntityType,
        string? sourceEntityId,
        string? requestedByUserId,
        string? deduplicationKey,
        CancellationToken cancellationToken)
    {
        ISocialPublisher? publisher = this.publishers.FirstOrDefault(candidate => candidate.Network == network);
        if (publisher is null)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.InvalidNetwork());
        }

        string normalizedMessage = message?.Trim() ?? string.Empty;
        if (normalizedMessage.Length == 0 || normalizedMessage.Length > MaximumMessageLength)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.InvalidMessage());
        }

        string? normalizedUrl = await this.NormalizePublicSiteUrlAsync(url, cancellationToken);
        if (normalizedUrl is null)
        {
            return ApplicationResult<SocialPublication>.Failure(SocialPublishingApplicationErrors.InvalidUrl());
        }

        DateTime requestedAtUtc = DateTime.UtcNow;
        SocialPublication publication = new SocialPublication
        {
            Id = Guid.NewGuid().ToString("N"),
            Network = network,
            Status = SocialPublicationStatus.Pending,
            Trigger = trigger,
            Message = normalizedMessage,
            Url = normalizedUrl,
            SourceEntityType = sourceEntityType,
            SourceEntityId = sourceEntityId,
            RequestedByUserId = requestedByUserId,
            DeduplicationKey = deduplicationKey,
            RequestedAtUtc = requestedAtUtc,
            CreatedAtUtc = requestedAtUtc,
            UpdatedAtUtc = requestedAtUtc,
        };

        publication = await this.repository.CreateAsync(publication, cancellationToken);
        publication = await this.AttemptPublicationAsync(publication, cancellationToken);
        return ApplicationResult<SocialPublication>.Success(publication);
    }

    private async Task<SocialPublication> AttemptPublicationAsync(SocialPublication publication, CancellationToken cancellationToken)
    {
        ISocialPublisher? publisher = this.publishers.FirstOrDefault(candidate => candidate.Network == publication.Network);
        SocialPublisherDescriptor? descriptor = publisher?.Describe();
        publication.AttemptedAtUtc = DateTime.UtcNow;

        if (publisher is null || descriptor is null || !descriptor.IsEnabled || !descriptor.IsConfigured)
        {
            publication.Status = SocialPublicationStatus.Failed;
            publication.FailureCode = "publisher-not-configured";
            publication.FailureMessage = "Le canal de publication n'est pas encore configuré.";
            publication.Touch();
            return await this.repository.UpdateAsync(publication, cancellationToken);
        }

        SocialPublisherResult result;
        try
        {
            result = await publisher.PublishLinkAsync(
                new SocialPublisherRequest(publication.Message, publication.Url),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            result = new SocialPublisherResult(
                false,
                null,
                null,
                "publisher-unavailable",
                "Le réseau social n'a pas pu être contacté.");
        }

        publication.Status = result.IsSuccess ? SocialPublicationStatus.Published : SocialPublicationStatus.Failed;
        publication.PublishedAtUtc = result.IsSuccess ? DateTime.UtcNow : null;
        publication.ExternalPostId = result.ExternalPostId;
        publication.ExternalPostUrl = result.ExternalPostUrl;
        publication.FailureCode = result.FailureCode;
        publication.FailureMessage = result.FailureMessage;
        publication.Touch();
        return await this.repository.UpdateAsync(publication, cancellationToken);
    }

    private Task PreservePublicPageDuringSocialPreparationAsync(string path, CancellationToken cancellationToken)
    {
        return this.ssrPageCacheInvalidator.InvalidateAsync(
            new SsrPageCacheInvalidationRequest
            {
                Paths = new[] { path },
                IncludeSeoDocuments = false,
                AllowStale = true,
                Refresh = false,
            },
            cancellationToken);
    }

    private static bool CanManage(SocialPublication publication)
    {
        return publication.Status == SocialPublicationStatus.Published
            && !string.IsNullOrWhiteSpace(publication.ExternalPostId);
    }

    private static string? NormalizeExternalMessage(string? message)
    {
        if (message is null)
        {
            return null;
        }

        string normalizedMessage = message.Trim();
        if (normalizedMessage.Length <= MaximumMessageLength)
        {
            return normalizedMessage;
        }

        int truncatedLength = MaximumMessageLength;
        if (char.IsHighSurrogate(normalizedMessage[truncatedLength - 1])
            && char.IsLowSurrogate(normalizedMessage[truncatedLength]))
        {
            truncatedLength--;
        }

        return normalizedMessage[..truncatedLength];
    }

    private ISocialPublisher? FindPublisher(SocialNetwork network)
    {
        return this.publishers.FirstOrDefault(candidate => candidate.Network == network);
    }

    private async Task<SocialPublication> MarkDeletedAsync(
        SocialPublication publication,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        DateTime deletedAtUtc = DateTime.UtcNow;
        publication.Status = SocialPublicationStatus.Deleted;
        publication.DeletedAtUtc = deletedAtUtc;
        publication.LastSynchronizedAtUtc = deletedAtUtc;
        publication.RequestedByUserId = requestedByUserId;
        publication.Touch();
        return await this.repository.UpdateAsync(publication, cancellationToken);
    }

    private async Task<string?> NormalizePublicSiteUrlAsync(string? value, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? candidate))
        {
            return null;
        }

        PublicSeoContext context = await this.publicSeoContextProvider.GetAsync(cancellationToken);
        if (!Uri.TryCreate(context.PublicBaseUrl, UriKind.Absolute, out Uri? publicBaseUri)
            || !string.Equals(candidate.Scheme, publicBaseUri.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(candidate.Host, publicBaseUri.Host, StringComparison.OrdinalIgnoreCase)
            || candidate.Port != publicBaseUri.Port)
        {
            return null;
        }

        UriBuilder normalized = new UriBuilder(candidate)
        {
            Fragment = string.Empty,
        };
        return normalized.Uri.AbsoluteUri;
    }
}
