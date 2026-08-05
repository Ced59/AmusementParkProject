using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class SocialPublicationService : ISocialPublicationService
{
    internal const int MaximumMessageLength = 5000;

    private readonly ISocialPublicationRepository repository;
    private readonly IReadOnlyCollection<ISocialPublisher> publishers;
    private readonly IPublicSeoContextProvider publicSeoContextProvider;

    public SocialPublicationService(
        ISocialPublicationRepository repository,
        IEnumerable<ISocialPublisher> publishers,
        IPublicSeoContextProvider publicSeoContextProvider)
    {
        this.repository = repository;
        this.publishers = publishers.ToList();
        this.publicSeoContextProvider = publicSeoContextProvider;
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
        string url = $"{context.PublicBaseUrl.TrimEnd('/')}/fr/park/{Uri.EscapeDataString(park.Id!)}/{parkSlug}";
        string message = BuildParkAnnouncementMessage(park.Name!);

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

    internal static string BuildParkAnnouncementMessage(string parkName)
    {
        return $"🎢 Un nouveau parc est disponible sur Amusement Parks : {parkName} !\n"
            + "Découvre sa fiche dès maintenant.\n\n"
            + $"🇬🇧 A new park is now available on Amusement Parks: {parkName}!\n"
            + "Discover its page now.";
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
