using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;
using AmusementPark.Application.Features.FeatureFlags.Results;
using AmusementPark.Application.Features.FeatureFlags.Services;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.FeatureFlags.Services;

public sealed class PublicFeatureCapabilityReaderTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public async Task ReadAsync_ShouldRequireTheFlagPublicReadAndConfiguredPublicTarget(
        bool flagEnabled, bool publicReadEnabled, bool hasPublicTarget, bool expectedEnabled)
    {
        Mock<IFeatureFlagEvaluator> evaluator = new Mock<IFeatureFlagEvaluator>(MockBehavior.Strict);
        evaluator.Setup(value => value.EvaluateAsync(FeatureFlagKeys.LivePublicExperience, CancellationToken.None))
            .ReturnsAsync(new FeatureFlagEvaluation(FeatureFlagKeys.LivePublicExperience, flagEnabled, FeatureFlagEvaluationSource.Default, 0));
        Mock<ILiveDataSourceCatalog> sourceCatalog = new Mock<ILiveDataSourceCatalog>();
        sourceCatalog.SetupGet(value => value.IsPublicReadEnabled).Returns(publicReadEnabled);
        sourceCatalog.SetupGet(value => value.PublicPollingTarget).Returns(hasPublicTarget ? CreateTarget() : null);
        PublicFeatureCapabilityReader reader = new PublicFeatureCapabilityReader(new FeatureFlagCatalog(), evaluator.Object, sourceCatalog.Object);

        IReadOnlyCollection<PublicFeatureCapabilityResult> results = await reader.ReadAsync(CancellationToken.None);

        PublicFeatureCapabilityResult result = Assert.Single(results);
        Assert.Equal(FeatureFlagKeys.LivePublicExperience, result.Key);
        Assert.Equal(expectedEnabled, result.IsEnabled);
        evaluator.VerifyAll();
    }

    private static LivePollingTarget CreateTarget()
    {
        return new LivePollingTarget(
            LiveDataSourceId.Parse("themeparks-wiki"), "external-park-1",
            new LivePollingActiveWindow(TimeZoneInfo.Utc, 6, 23),
            new LivePollingPolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), TimeSpan.FromHours(1), 5, TimeSpan.FromMinutes(30)),
            TimeSpan.Zero);
    }
}
