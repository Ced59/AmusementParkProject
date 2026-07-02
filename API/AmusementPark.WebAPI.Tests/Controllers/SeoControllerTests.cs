using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class SeoControllerTests
{
    [Fact]
    public void GetRobotsTxt_WhenPublicImageAllowPathIsConfigured_ShouldAllowBinaryImagesAndKeepApiDisallowed()
    {
        SeoController controller = CreateController(new SeoSettings
        {
            PublicBaseUrl = "https://amusement-parks.fun",
            SupportedLanguages = new List<string> { "en", "fr" },
            RobotsAllowPaths = new List<string> { "/api/images/binary/" },
            RobotsDisallowPaths = new List<string> { "/api/", "/{lang}/admin/" },
        });

        ContentResult result = Assert.IsType<ContentResult>(controller.GetRobotsTxt());
        string content = Assert.IsType<string>(result.Content);
        string[] lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains("Allow: /", lines);
        Assert.Contains("Allow: /api/images/binary/", lines);
        Assert.DoesNotContain("Allow: /api/images/", lines);
        Assert.Contains("Disallow: /api/", lines);
        Assert.Contains("Disallow: /en/admin/", lines);
        Assert.Contains("Disallow: /fr/admin/", lines);
        Assert.Contains("Sitemap: https://amusement-parks.fun/sitemap.xml", lines);
        Assert.True(content.IndexOf("Allow: /api/images/binary/", StringComparison.Ordinal) < content.IndexOf("Disallow: /api/", StringComparison.Ordinal));
    }

    [Fact]
    public void GetPublicHtmlSitemapNodesAsync_ShouldUseDataInvalidatedCachePolicy()
    {
        OutputCacheAttribute attribute = Assert.Single(
            typeof(SeoController)
                .GetMethod(nameof(SeoController.GetPublicHtmlSitemapNodesAsync))!
                .GetCustomAttributes(typeof(OutputCacheAttribute), inherit: false)
                .Cast<OutputCacheAttribute>());

        Assert.Equal(ApiOutputCachePolicyNames.PublicHtmlSitemapNodes, attribute.PolicyName);
    }

    [Fact]
    public async Task GetIndexNowKeyFileAsync_WhenCustomRootLocationIsConfigured_ShouldServeKey()
    {
        Mock<ISeoSitemapSettingsRepository> sitemapSettingsRepository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        sitemapSettingsRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeoSitemapSettings
            {
                IsIndexNowEnabled = true,
                IndexNowKey = "secret-key",
                IndexNowKeyLocation = "/indexnow-key.txt",
            });
        SeoController controller = CreateController(
            new SeoSettings { PublicBaseUrl = "https://amusement-parks.fun" },
            sitemapSettingsRepository.Object);

        IActionResult actionResult = await controller.GetIndexNowKeyFileAsync("indexnow-key");

        ContentResult result = Assert.IsType<ContentResult>(actionResult);
        Assert.Equal("secret-key", result.Content);
        Assert.Equal("text/plain; charset=utf-8", result.ContentType);
        sitemapSettingsRepository.VerifyAll();
    }

    private static SeoController CreateController(SeoSettings settings, ISeoSitemapSettingsRepository? sitemapSettingsRepository = null)
    {
        Mock<IWebHostEnvironment> environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(static candidate => candidate.EnvironmentName).Returns(Environments.Production);

        ISeoSitemapSettingsRepository resolvedSitemapSettingsRepository = sitemapSettingsRepository ?? new Mock<ISeoSitemapSettingsRepository>().Object;
        Mock<IQueryHandler<GetPublicSitemapDocumentQuery, ApplicationResult<SitemapDocumentResult>>> sitemapDocumentHandler = new Mock<IQueryHandler<GetPublicSitemapDocumentQuery, ApplicationResult<SitemapDocumentResult>>>();
        Mock<IQueryHandler<GetPublicHtmlSitemapNodesQuery, ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>>> htmlSitemapNodesHandler = new Mock<IQueryHandler<GetPublicHtmlSitemapNodesQuery, ApplicationResult<IReadOnlyCollection<PublicHtmlSitemapNode>>>>();

        return new SeoController(
            Options.Create(settings),
            environment.Object,
            resolvedSitemapSettingsRepository,
            sitemapDocumentHandler.Object,
            htmlSitemapNodesHandler.Object);
    }
}
