using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;
using AmusementPark.Application.Features.FeatureFlags.Results;
using AmusementPark.Application.Features.FeatureFlags.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.FeatureFlags.Services;

public sealed class FeatureFlagAdministrationServiceTests
{
    [Fact]
    public async Task UpdateAsync_ShouldAppendAuditableRevision()
    {
        Mock<IFeatureFlagStateRepository> repository = CreateRepository();
        repository.Setup(value => value.GetLatestAsync(
                FeatureFlagKeys.LivePublicExperience,
                CancellationToken.None))
            .ReturnsAsync((FeatureFlagState?)null);
        repository.Setup(value => value.AppendRevisionAsync(
                It.Is<FeatureFlagState>(state =>
                    state.Key == FeatureFlagKeys.LivePublicExperience
                    && state.EnabledOverride == false
                    && state.Revision == 1
                    && state.ChangedByUserId == "admin-1"),
                0,
                CancellationToken.None))
            .ReturnsAsync(FeatureFlagWriteOutcome.Created);
        Mock<IFeatureFlagEvaluator> evaluator = new Mock<IFeatureFlagEvaluator>(MockBehavior.Strict);
        evaluator.Setup(value => value.EvaluateAsync(
                FeatureFlagKeys.LivePublicExperience,
                CancellationToken.None))
            .ReturnsAsync(new FeatureFlagEvaluation(
                FeatureFlagKeys.LivePublicExperience,
                false,
                FeatureFlagEvaluationSource.Override,
                1));
        FeatureFlagAdministrationService service = new FeatureFlagAdministrationService(
            new FeatureFlagCatalog(),
            repository.Object,
            evaluator.Object);

        ApplicationResult<FeatureFlagAdministrationResult> result = await service.UpdateAsync(
            FeatureFlagKeys.LivePublicExperience,
            false,
            0,
            "Incident fournisseur confirmé.",
            "admin-1",
            CancellationToken.None);

        FeatureFlagAdministrationResult value = Assert.IsType<FeatureFlagAdministrationResult>(result.Value);
        Assert.False(value.IsEnabled);
        Assert.Equal(1, value.Revision);
        repository.VerifyAll();
        evaluator.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenExpectedRevisionIsStale_ShouldReturnConflict()
    {
        FeatureFlagState current = new FeatureFlagState(
            Guid.NewGuid(),
            FeatureFlagKeys.LivePublicExperience,
            "Production",
            false,
            2,
            1,
            "admin-1",
            "Existing operational decision.",
            DateTime.UtcNow);
        Mock<IFeatureFlagStateRepository> repository = CreateRepository();
        repository.Setup(value => value.GetLatestAsync(
                FeatureFlagKeys.LivePublicExperience,
                CancellationToken.None))
            .ReturnsAsync(current);
        Mock<IFeatureFlagEvaluator> evaluator = new Mock<IFeatureFlagEvaluator>(MockBehavior.Strict);
        FeatureFlagAdministrationService service = new FeatureFlagAdministrationService(
            new FeatureFlagCatalog(),
            repository.Object,
            evaluator.Object);

        ApplicationResult<FeatureFlagAdministrationResult> result = await service.UpdateAsync(
            FeatureFlagKeys.LivePublicExperience,
            true,
            1,
            "Attempt based on a stale screen.",
            "admin-2",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("feature-flag.conflict", error.Code);
        Assert.Equal(2, error.CurrentVersion);
        repository.Verify(value => value.GetLatestAsync(
            FeatureFlagKeys.LivePublicExperience,
            CancellationToken.None), Times.Once);
        evaluator.VerifyNoOtherCalls();
    }

    private static Mock<IFeatureFlagStateRepository> CreateRepository()
    {
        Mock<IFeatureFlagStateRepository> repository =
            new Mock<IFeatureFlagStateRepository>(MockBehavior.Strict);
        repository.SetupGet(value => value.Environment).Returns("Production");
        return repository;
    }
}
