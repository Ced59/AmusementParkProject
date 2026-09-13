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

internal sealed class RecordingSsrPageCacheInvalidator : ISsrPageCacheInvalidator
{
    private readonly ICollection<string>? events;

    public RecordingSsrPageCacheInvalidator(ICollection<string>? events = null)
    {
        this.events = events;
    }

    public List<SsrPageCacheInvalidationRequest> Requests { get; } = new List<SsrPageCacheInvalidationRequest>();

    public Task InvalidateAsync(
        SsrPageCacheInvalidationRequest request,
        CancellationToken cancellationToken = default)
    {
        this.events?.Add("invalidate");
        this.Requests.Add(request);
        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        return this.InvalidateAsync(SsrPageCacheInvalidationRequest.AllCaches(), cancellationToken);
    }
}
