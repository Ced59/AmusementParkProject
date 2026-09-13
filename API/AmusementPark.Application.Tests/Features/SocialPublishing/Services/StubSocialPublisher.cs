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

internal sealed class StubSocialPublisher : ISocialPublisher
{
    private readonly SocialPublisherDescriptor descriptor;
    private readonly ICollection<string>? events;

    private StubSocialPublisher(
        SocialPublisherDescriptor descriptor,
        ICollection<string>? events = null)
    {
        this.descriptor = descriptor;
        this.events = events;
    }

    public SocialNetwork Network => SocialNetwork.Facebook;

    public int PublishCallCount { get; private set; }

    public int UpdateCallCount { get; private set; }

    public int RefreshPreviewCallCount { get; private set; }

    public int DeleteCallCount { get; private set; }

    public int GetCallCount { get; private set; }

    public int ReconcileCallCount { get; private set; }

    public SocialPublisherLinkReconciliationResult ReconciliationResult { get; set; } =
        new SocialPublisherLinkReconciliationResult(
            true,
            false,
            false,
            null,
            null,
            null,
            null);

    public Func<CancellationToken, Task<SocialPublisherLinkReconciliationResult>>? ReconcileAsync { get; set; }

    public Exception? PublishException { get; set; }

    public string? LastRefreshedUrl { get; private set; }

    public SocialPublisherOperationResult DeleteResult { get; set; } =
        new SocialPublisherOperationResult(true, false, null, null);

    public SocialPublisherPostSnapshotResult SnapshotResult { get; set; } =
        new SocialPublisherPostSnapshotResult(
            true,
            true,
            "Message",
            "https://www.facebook.com/123/posts/456",
            null,
            null);

    public static StubSocialPublisher Configured(ICollection<string>? events = null)
    {
        return new StubSocialPublisher(new SocialPublisherDescriptor(
            SocialNetwork.Facebook,
            "Facebook",
            true,
            true,
            "https://www.facebook.com/test",
            true), events);
    }

    public static StubSocialPublisher Disabled()
    {
        return new StubSocialPublisher(new SocialPublisherDescriptor(
            SocialNetwork.Facebook,
            "Facebook",
            false,
            false,
            null,
            true));
    }

    public SocialPublisherDescriptor Describe()
    {
        return this.descriptor;
    }

    public Task<SocialPublisherResult> PublishLinkAsync(SocialPublisherRequest request, CancellationToken cancellationToken)
    {
        this.events?.Add("publish");
        this.PublishCallCount++;
        if (this.PublishException is not null)
        {
            return Task.FromException<SocialPublisherResult>(this.PublishException);
        }

        return Task.FromResult(new SocialPublisherResult(
            true,
            "facebook-post-1",
            "https://www.facebook.com/facebook-post-1",
            null,
            null));
    }

    public Task<SocialPublisherLinkReconciliationResult> ReconcilePublishedLinkAsync(
        SocialPublisherLinkReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        this.ReconcileCallCount++;
        return this.ReconcileAsync is null
            ? Task.FromResult(this.ReconciliationResult)
            : this.ReconcileAsync(cancellationToken);
    }

    public Task<SocialPublisherOperationResult> UpdatePostAsync(
        string externalPostId,
        string message,
        CancellationToken cancellationToken)
    {
        this.UpdateCallCount++;
        return Task.FromResult(new SocialPublisherOperationResult(true, false, null, null));
    }

    public Task<SocialPublisherOperationResult> RefreshLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken)
    {
        this.events?.Add("refresh");
        this.RefreshPreviewCallCount++;
        this.LastRefreshedUrl = url;
        return Task.FromResult(new SocialPublisherOperationResult(true, false, null, null));
    }

    public Task<SocialPublisherOperationResult> DeletePostAsync(
        string externalPostId,
        CancellationToken cancellationToken)
    {
        this.DeleteCallCount++;
        return Task.FromResult(this.DeleteResult);
    }

    public Task<SocialPublisherPostSnapshotResult> GetPostAsync(
        string externalPostId,
        CancellationToken cancellationToken)
    {
        this.GetCallCount++;
        return Task.FromResult(this.SnapshotResult);
    }
}
