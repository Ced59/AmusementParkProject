using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalEditorialTransitionPolicyTests
{
    [Theory]
    [InlineData(HistoricalReviewResourceType.Fact)]
    [InlineData(HistoricalReviewResourceType.Relation)]
    public void GetNextStage_ForAssertedResource_ShouldRequireEvidenceBeforeReview(
        HistoricalReviewResourceType resourceType)
    {
        HistoricalEditorialWorkflowState nextStage =
            HistoricalEditorialTransitionPolicy.GetNextStage(
                resourceType,
                HistoricalEditorialWorkflowState.Draft);

        Assert.Equal(HistoricalEditorialWorkflowState.SourcesAttached, nextStage);
    }

    [Fact]
    public void GetNextStage_ForSource_ShouldEnterEditorialReviewDirectly()
    {
        HistoricalEditorialWorkflowState nextStage =
            HistoricalEditorialTransitionPolicy.GetNextStage(
                HistoricalReviewResourceType.Source,
                HistoricalEditorialWorkflowState.Draft);

        Assert.Equal(HistoricalEditorialWorkflowState.EditorialReview, nextStage);
    }

    [Theory]
    [InlineData(HistoricalEditorialWorkflowState.SourcesAttached, HistoricalEditorialWorkflowState.EditorialReview)]
    [InlineData(HistoricalEditorialWorkflowState.EditorialReview, HistoricalEditorialWorkflowState.StructuredValidation)]
    [InlineData(HistoricalEditorialWorkflowState.StructuredValidation, HistoricalEditorialWorkflowState.Published)]
    public void GetNextStage_ShouldAdvanceOneReviewGateAtATime(
        HistoricalEditorialWorkflowState currentStage,
        HistoricalEditorialWorkflowState expectedStage)
    {
        HistoricalEditorialWorkflowState nextStage =
            HistoricalEditorialTransitionPolicy.GetNextStage(
                HistoricalReviewResourceType.Fact,
                currentStage);

        Assert.Equal(expectedStage, nextStage);
    }

    [Fact]
    public void GetNextStage_FromPublishedResource_ShouldRejectFurtherAdvance()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalEditorialTransitionPolicy.GetNextStage(
                    HistoricalReviewResourceType.Fact,
                    HistoricalEditorialWorkflowState.Published));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidReviewEvent, exception.ErrorCode);
    }

    [Fact]
    public void GetSaveEventType_ForPublishedResource_ShouldRecordCorrection()
    {
        HistoricalReviewEventType eventType = HistoricalEditorialTransitionPolicy.GetSaveEventType(
            HistoricalEditorialWorkflowState.Published);

        Assert.Equal(HistoricalReviewEventType.Corrected, eventType);
    }
}
