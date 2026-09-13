using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Services;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialPublishing.Services;

internal sealed class InMemorySocialPublicationRepository : ISocialPublicationRepository
{
    public List<SocialPublication> Publications { get; } = new List<SocialPublication>();

    public Task<SocialPublication> CreateAsync(SocialPublication publication, CancellationToken cancellationToken)
    {
        this.Publications.Add(publication);
        return Task.FromResult(publication);
    }

    public Task<SocialPublication> UpdateAsync(SocialPublication publication, CancellationToken cancellationToken)
    {
        int index = this.Publications.FindIndex(current => current.Id == publication.Id);
        if (index >= 0)
        {
            this.Publications[index] = publication;
        }

        return Task.FromResult(publication);
    }

    public Task<SocialPublication?> TryClaimFailedForRetryAsync(
        string publicationId,
        DateTime expectedUpdatedAtUtc,
        string? requestedByUserId,
        CancellationToken cancellationToken)
    {
        lock (this.Publications)
        {
            SocialPublication? publication = this.Publications.FirstOrDefault(current =>
                current.Id == publicationId
                && current.Status == SocialPublicationStatus.Failed
                && current.UpdatedAtUtc == expectedUpdatedAtUtc);
            if (publication is null)
            {
                return Task.FromResult<SocialPublication?>(null);
            }

            publication.RequestedByUserId = requestedByUserId;
            publication.Status = SocialPublicationStatus.Pending;
            publication.FailureCode = null;
            publication.FailureMessage = null;
            publication.Touch();
            return Task.FromResult<SocialPublication?>(publication);
        }
    }

    public Task<SocialPublication?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.Publications.FirstOrDefault(publication => publication.Id == id));
    }

    public Task<SocialPublication?> GetByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.Publications.FirstOrDefault(
            publication => publication.DeduplicationKey == deduplicationKey));
    }

    public Task<SocialPublication?> GetByExternalPostIdAsync(string externalPostId, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.Publications.FirstOrDefault(
            publication => publication.ExternalPostId == externalPostId));
    }

    public Task<IReadOnlyCollection<SocialPublication>> ListRecentAsync(int limit, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<SocialPublication> publications = this.Publications.Take(limit).ToList();
        return Task.FromResult(publications);
    }

    public Task<IReadOnlyCollection<string>> ListPublishedAutomaticParkAnnouncementParkIdsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> parkIds = this.Publications
            .Where(static publication => publication.Network == SocialNetwork.Facebook
                && publication.Status == SocialPublicationStatus.Published
                && publication.Trigger == SocialPublicationTrigger.AutomaticParkPublication
                && !string.IsNullOrWhiteSpace(publication.ExternalPostId)
                && !string.IsNullOrWhiteSpace(publication.SourceEntityId))
            .Select(static publication => publication.SourceEntityId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(parkIds);
    }
}
