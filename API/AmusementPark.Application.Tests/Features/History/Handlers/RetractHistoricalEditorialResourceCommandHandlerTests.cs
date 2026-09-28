using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class RetractHistoricalEditorialResourceCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new DateTimeOffset(2026, 9, 28, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenPublishedFactIsRetracted_ShouldRefreshSitemap()
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc exemple",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalFact fact = PublicParkHistoryTestData.CreateOpeningFact(subject, 2001);
        Mock<IHistoricalFactRepository> facts = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalRelationRepository> relations =
            new Mock<IHistoricalRelationRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sources =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler =
            new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        facts.Setup(repository => repository.GetLatestRevisionAsync(
                fact.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        facts.Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(candidate =>
                    candidate.Id == fact.Id
                    && candidate.Revision == fact.Revision + 1
                    && candidate.WorkflowState == HistoricalEditorialWorkflowState.Retracted
                    && candidate.PublicationState == HistoricalPublicationState.Withdrawn),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        sitemapRefreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        RetractHistoricalEditorialResourceCommandHandler handler =
            new RetractHistoricalEditorialResourceCommandHandler(
                facts.Object,
                relations.Object,
                sources.Object,
                sitemapRefreshScheduler.Object,
                new FixedHistoryEditorialTimeProvider(Now));

        ApplicationResult<HistoricalEditorialMutationResult> result = await handler.HandleAsync(
            new RetractHistoricalEditorialResourceCommand(
                HistoricalReviewResourceType.Fact,
                fact.Id,
                fact.Revision,
                "admin-1",
                "Retrait public"));

        Assert.True(result.IsSuccess);
        Assert.Equal(HistoricalPublicationState.Withdrawn, result.Value?.PublicationState);
        facts.VerifyAll();
        relations.VerifyNoOtherCalls();
        sources.VerifyNoOtherCalls();
        sitemapRefreshScheduler.VerifyAll();
    }
}
