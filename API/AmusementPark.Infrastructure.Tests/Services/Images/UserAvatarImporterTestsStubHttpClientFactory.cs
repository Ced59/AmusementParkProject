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

internal sealed class UserAvatarImporterTestsStubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient httpClient;

    public UserAvatarImporterTestsStubHttpClientFactory(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public HttpClient CreateClient(string name)
    {
        return this.httpClient;
    }
}
