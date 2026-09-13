using System.Net;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Services.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Images;

internal sealed class AvatarHttpMessageHandler : HttpMessageHandler
{
    private readonly byte[] content;

    public AvatarHttpMessageHandler(byte[] content)
    {
        this.content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(this.content),
        };
        response.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        return Task.FromResult(response);
    }
}
