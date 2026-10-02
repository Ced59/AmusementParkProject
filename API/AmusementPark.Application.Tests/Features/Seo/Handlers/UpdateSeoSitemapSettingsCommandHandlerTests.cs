using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Commands;
using AmusementPark.Application.Features.Seo.Handlers;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Handlers;

public sealed class UpdateSeoSitemapSettingsCommandHandlerTests
{
    [Theory]
    [InlineData("sitemap-static-fr", "")]
    [InlineData("SITEMAP-STATIC-FR", "")]
    [InlineData("existing-key", "/sitemap-static-fr.txt")]
    [InlineData("existing-key", "sitemap-static-fr.txt")]
    [InlineData("existing-key", " /SITEMAP-STATIC-FR.txt ")]
    [InlineData("existing-key", "/sitemap-static-fr.txt?version=1#fragment")]
    [InlineData("existing-key", "https://amusement-parks.fun/sitemap-static-fr.txt")]
    [InlineData("existing-key", "https://amusement-parks.fun/sitemap-static-fr.txt?version=1#fragment")]
    [InlineData("existing-key", "https://amusement-parks.fun/%73itemap-static-fr.txt")]
    [InlineData("existing-key", "verification/../sitemap-static-fr.txt")]
    [InlineData("existing-key", "/verification/%2e%2e/sitemap-static-fr.txt")]
    public async Task HandleAsync_WhenEnabledKeyWouldShadowTextSitemap_ShouldRejectWithoutSaving(string key, string location)
    {
        Mock<ISeoSitemapSettingsRepository> repository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        UpdateSeoSitemapSettingsCommandHandler handler = new UpdateSeoSitemapSettingsCommandHandler(repository.Object);

        ApplicationResult<SeoSitemapSettings> result = await handler.HandleAsync(
            new UpdateSeoSitemapSettingsCommand(true, false, false, key, location, Array.Empty<string>()));

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("seo.indexnow.reserved-key-path", error.Code);
        Assert.Equal(ApplicationErrorType.Validation, error.Type);
        repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true, "existing-key", "")]
    [InlineData(true, "existing-key", "/existing-verification.txt")]
    [InlineData(true, "existing-key", "https://amusement-parks.fun/existing-verification.txt")]
    [InlineData(false, "sitemap-static-fr", "/sitemap-static-fr.txt")]
    public async Task HandleAsync_WhenKeyDoesNotServeReservedRoute_ShouldPreserveSettings(bool enabled, string key, string location)
    {
        Mock<ISeoSitemapSettingsRepository> repository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        repository.Setup(value => value.SaveAsync(It.IsAny<SeoSitemapSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        UpdateSeoSitemapSettingsCommandHandler handler = new UpdateSeoSitemapSettingsCommandHandler(repository.Object);

        ApplicationResult<SeoSitemapSettings> result = await handler.HandleAsync(
            new UpdateSeoSitemapSettingsCommand(enabled, false, false, key, location, Array.Empty<string>()));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(key, result.Value.IndexNowKey);
        Assert.Equal(location, result.Value.IndexNowKeyLocation);
        repository.Verify(value => value.SaveAsync(It.IsAny<SeoSitemapSettings>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyNoOtherCalls();
    }
}
