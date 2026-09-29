using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;
using AmusementPark.Application.Features.FeatureFlags.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.FeatureFlags.Services;

public sealed class FeatureFlagEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_WithoutOverride_ShouldUseCatalogDefault()
    {
        Mock<IFeatureFlagStateRepository> repository = CreateRepository();
        repository.Setup(value => value.GetLatestAsync(
                FeatureFlagKeys.LivePublicExperience,
                CancellationToken.None))
            .ReturnsAsync((FeatureFlagState?)null);
        FeatureFlagEvaluator evaluator = CreateEvaluator(repository.Object);

        FeatureFlagEvaluation result = await evaluator.EvaluateAsync(
            FeatureFlagKeys.LivePublicExperience,
            CancellationToken.None);

        Assert.True(result.IsEnabled);
        Assert.Equal(FeatureFlagEvaluationSource.Default, result.Source);
        Assert.Equal(0, result.Revision);
    }

    [Fact]
    public async Task EvaluateAsync_WithOverride_ShouldUseLatestOverride()
    {
        Mock<IFeatureFlagStateRepository> repository = CreateRepository();
        repository.Setup(value => value.GetLatestAsync(
                FeatureFlagKeys.LivePublicExperience,
                CancellationToken.None))
            .ReturnsAsync(new FeatureFlagState(
                Guid.NewGuid(),
                FeatureFlagKeys.LivePublicExperience,
                "Production",
                false,
                3,
                2,
                "admin-1",
                "Temporary incident response.",
                DateTime.UtcNow));
        FeatureFlagEvaluator evaluator = CreateEvaluator(repository.Object);

        FeatureFlagEvaluation result = await evaluator.EvaluateAsync(
            FeatureFlagKeys.LivePublicExperience,
            CancellationToken.None);

        Assert.False(result.IsEnabled);
        Assert.Equal(FeatureFlagEvaluationSource.Override, result.Source);
        Assert.Equal(3, result.Revision);
    }

    [Fact]
    public async Task EvaluateAsync_WhenStorageFails_ShouldUseSafeFallback()
    {
        Mock<IFeatureFlagStateRepository> repository = CreateRepository();
        repository.Setup(value => value.GetLatestAsync(
                FeatureFlagKeys.LivePublicExperience,
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("unavailable"));
        FeatureFlagEvaluator evaluator = CreateEvaluator(repository.Object);

        FeatureFlagEvaluation result = await evaluator.EvaluateAsync(
            FeatureFlagKeys.LivePublicExperience,
            CancellationToken.None);

        Assert.False(result.IsEnabled);
        Assert.Equal(FeatureFlagEvaluationSource.SafeFallback, result.Source);
    }

    [Fact]
    public async Task EvaluateAsync_WhenKeyIsUnknown_ShouldStayDisabled()
    {
        Mock<IFeatureFlagStateRepository> repository = CreateRepository();
        FeatureFlagEvaluator evaluator = CreateEvaluator(repository.Object);

        FeatureFlagEvaluation result = await evaluator.EvaluateAsync(
            "unknown:flag",
            CancellationToken.None);

        Assert.False(result.IsEnabled);
        Assert.Equal(FeatureFlagEvaluationSource.SafeFallback, result.Source);
        repository.VerifyNoOtherCalls();
    }

    private static FeatureFlagEvaluator CreateEvaluator(IFeatureFlagStateRepository repository)
    {
        return new FeatureFlagEvaluator(
            new FeatureFlagCatalog(),
            repository,
            NullLogger<FeatureFlagEvaluator>.Instance);
    }

    private static Mock<IFeatureFlagStateRepository> CreateRepository()
    {
        Mock<IFeatureFlagStateRepository> repository =
            new Mock<IFeatureFlagStateRepository>(MockBehavior.Strict);
        repository.SetupGet(value => value.Environment).Returns("Production");
        return repository;
    }
}
