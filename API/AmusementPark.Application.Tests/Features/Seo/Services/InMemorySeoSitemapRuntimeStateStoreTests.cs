using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class InMemorySeoSitemapRuntimeStateStoreTests
{
    [Fact]
    public void GetCurrent_WhenNewStore_ShouldReturnIdleState()
    {
        InMemorySeoSitemapRuntimeStateStore store = new InMemorySeoSitemapRuntimeStateStore();

        SitemapRuntimeState result = store.GetCurrent();

        Assert.Equal(SitemapGenerationStatus.Idle, result.Status);
        Assert.Equal("idle", result.CurrentStep);
        Assert.Equal(0, result.ProgressPercentage);
    }

    [Fact]
    public void TryStart_WhenIdle_ShouldSwitchToRunningAndReturnTrue()
    {
        InMemorySeoSitemapRuntimeStateStore store = new InMemorySeoSitemapRuntimeStateStore();

        bool result = store.TryStart(" collecting ");
        SitemapRuntimeState state = store.GetCurrent();

        Assert.True(result);
        Assert.Equal(SitemapGenerationStatus.Running, state.Status);
        Assert.Equal("collecting", state.CurrentStep);
        Assert.Equal(1, state.ProgressPercentage);
        Assert.NotNull(state.StartedAtUtc);
        Assert.NotNull(state.UpdatedAtUtc);
    }

    [Fact]
    public void TryStart_WhenAlreadyRunning_ShouldReturnFalseAndKeepCurrentState()
    {
        InMemorySeoSitemapRuntimeStateStore store = new InMemorySeoSitemapRuntimeStateStore();
        Assert.True(store.TryStart("first"));

        bool result = store.TryStart("second");

        Assert.False(result);
        Assert.Equal("first", store.GetCurrent().CurrentStep);
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(50, 50)]
    [InlineData(150, 100)]
    public void Update_WhenProgressIsOutsideBounds_ShouldClampProgress(int progress, int expected)
    {
        InMemorySeoSitemapRuntimeStateStore store = new InMemorySeoSitemapRuntimeStateStore();
        store.TryStart("start");

        store.Update("writing", progress, "message");

        SitemapRuntimeState state = store.GetCurrent();
        Assert.Equal(SitemapGenerationStatus.Running, state.Status);
        Assert.Equal("writing", state.CurrentStep);
        Assert.Equal(expected, state.ProgressPercentage);
        Assert.Equal("message", state.Message);
    }

    [Fact]
    public void Complete_WhenCalled_ShouldSwitchToSucceededWithFullProgress()
    {
        InMemorySeoSitemapRuntimeStateStore store = new InMemorySeoSitemapRuntimeStateStore();
        store.TryStart("start");

        store.Complete("done", "ok");

        SitemapRuntimeState state = store.GetCurrent();
        Assert.Equal(SitemapGenerationStatus.Succeeded, state.Status);
        Assert.Equal("done", state.CurrentStep);
        Assert.Equal(100, state.ProgressPercentage);
        Assert.Equal("ok", state.Message);
    }

    [Fact]
    public void Fail_WhenCalled_ShouldSwitchToFailedAndKeepProgress()
    {
        InMemorySeoSitemapRuntimeStateStore store = new InMemorySeoSitemapRuntimeStateStore();
        store.TryStart("start");
        store.Update("middle", 42);

        store.Fail("failed", "boom");

        SitemapRuntimeState state = store.GetCurrent();
        Assert.Equal(SitemapGenerationStatus.Failed, state.Status);
        Assert.Equal("failed", state.CurrentStep);
        Assert.Equal(42, state.ProgressPercentage);
        Assert.Equal("boom", state.Message);
    }
}
