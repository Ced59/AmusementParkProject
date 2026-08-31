using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.BackgroundJobs.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.BackgroundJobs.Services;

public sealed class DurableBackgroundJobHandlerRegistryTests
{
    [Fact]
    public void Constructor_WhenKindsAreDuplicated_ShouldRejectAmbiguousResolution()
    {
        StubHandler first = new StubHandler(CreateDefinition("same.kind"));
        StubHandler second = new StubHandler(CreateDefinition("same.kind"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new DurableBackgroundJobHandlerRegistry(new IDurableBackgroundJobHandler[] { first, second }));

        Assert.Contains("same.kind", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryResolve_WhenKindExists_ShouldReturnTheRegisteredHandler()
    {
        StubHandler expected = new StubHandler(CreateDefinition("test.kind"));
        DurableBackgroundJobHandlerRegistry registry =
            new DurableBackgroundJobHandlerRegistry(new[] { expected });

        bool found = registry.TryResolve("test.kind", out IDurableBackgroundJobHandler? actual);

        Assert.True(found);
        Assert.Same(expected, actual);
        Assert.Equal("test.kind", Assert.Single(registry.Definitions).Kind);
    }

    [Fact]
    public void Definition_WhenRetryBoundsAreInvalid_ShouldRejectThePolicy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DurableBackgroundJobHandlerDefinition(
            "test.kind",
            DurableBackgroundJobWorkload.Light,
            new[] { 1 },
            TimeSpan.FromMinutes(1),
            3,
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void RetryDelayCalculator_ShouldApplyExponentialBackoffAndBoundedJitter()
    {
        DurableBackgroundJobHandlerDefinition definition = new DurableBackgroundJobHandlerDefinition(
            "test.kind",
            DurableBackgroundJobWorkload.Light,
            new[] { 1 },
            TimeSpan.FromMinutes(1),
            10,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30));
        DurableBackgroundJobRetryDelayCalculator minimumCalculator =
            new DurableBackgroundJobRetryDelayCalculator(static () => 0);
        DurableBackgroundJobRetryDelayCalculator maximumCalculator =
            new DurableBackgroundJobRetryDelayCalculator(static () => 1);

        Assert.Equal(TimeSpan.FromSeconds(8), minimumCalculator.Calculate(definition, 1));
        Assert.Equal(TimeSpan.FromSeconds(12), maximumCalculator.Calculate(definition, 1));
        Assert.Equal(TimeSpan.FromSeconds(30), maximumCalculator.Calculate(definition, 10));
    }

    private static DurableBackgroundJobHandlerDefinition CreateDefinition(string kind)
    {
        return new DurableBackgroundJobHandlerDefinition(
            kind,
            DurableBackgroundJobWorkload.Light,
            new[] { 1 },
            TimeSpan.FromMinutes(1),
            3,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(1));
    }

    private sealed class StubHandler : IDurableBackgroundJobHandler
    {
        public StubHandler(DurableBackgroundJobHandlerDefinition definition)
        {
            this.Definition = definition;
        }

        public DurableBackgroundJobHandlerDefinition Definition { get; }

        public Task<DurableBackgroundJobHandlerResult> HandleAsync(
            DurableBackgroundJobExecutionContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(DurableBackgroundJobHandlerResult.Success());
        }
    }
}
