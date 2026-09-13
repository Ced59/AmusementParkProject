using System.Net;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;
using AmusementPark.Infrastructure.Services.SocialPublishing;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.SocialPublishing;

internal sealed class TrackingStringContent : StringContent
{
    private readonly Action onSerialize;

    public TrackingStringContent(string content, Action onSerialize)
        : base(content)
    {
        this.onSerialize = onSerialize;
    }

    protected override Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context)
    {
        this.onSerialize();
        return base.SerializeToStreamAsync(stream, context);
    }
}
