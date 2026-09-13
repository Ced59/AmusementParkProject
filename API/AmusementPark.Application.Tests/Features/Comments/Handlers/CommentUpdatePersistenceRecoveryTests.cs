using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Contracts;
using AmusementPark.Application.Features.Comments.Handlers;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Comments.Handlers;

public sealed class CommentUpdatePersistenceRecoveryTests
{
    [Fact]
    public async Task HandleAsync_WhenReplacementWasCommittedBeforeMongoThrows_ShouldFinalizeReservation()
    {
        using UpdateWithImageScenario scenario = new UpdateWithImageScenario();
        Comment? committed = null;
        scenario.SetupUpdateFailure(
            new InvalidOperationException("Acknowledgement lost."),
            (comment, expectedRevision) =>
            {
                comment.Revision = expectedRevision + 1;
                committed = comment;
            });
        scenario.SetupRecovery(() => committed);
        scenario.SetupFinalization();

        ApplicationResult<CommentResult> result =
            await scenario.Handler.HandleAsync(
                scenario.Command,
                scenario.OperationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(scenario.InitialRevision + 1, result.Value!.Revision);
        Assert.Equal(scenario.UpdatedHtml, Assert.Single(result.Value.Bodies).Value);
        scenario.VerifyAll();
        scenario.VerifyReservationWasNotReleased();
    }

    [Theory]
    [InlineData("absent")]
    [InlineData("revision")]
    [InlineData("content")]
    public async Task HandleAsync_WhenReplacementOutcomeIsAbsentOrInconsistent_ShouldKeepReservation(
        string outcome)
    {
        using UpdateWithImageScenario scenario = new UpdateWithImageScenario();
        scenario.SetupUpdateFailure(
            new InvalidOperationException("Persistence failed."),
            static (comment, expectedRevision) =>
                comment.Revision = expectedRevision + 1);
        scenario.SetupRecovery(() => outcome switch
        {
            "revision" => scenario.CreateRecoveryCandidate(
                scenario.Existing.Revision + 1,
                true),
            "content" => scenario.CreateRecoveryCandidate(
                scenario.Existing.Revision,
                false),
            _ => null,
        });

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => scenario.Handler.HandleAsync(
                    scenario.Command,
                    scenario.OperationToken));

        Assert.Equal("Persistence failed.", exception.Message);
        scenario.VerifyAll();
        scenario.VerifyReservationWasNotReleased();
        scenario.VerifyReservationWasNotFinalized();
    }

    [Fact]
    public async Task HandleAsync_WhenOutcomeIsUnknown_ShouldKeepDurablePublishedPreparation()
    {
        using UpdateWithImageScenario scenario =
            new UpdateWithImageScenario(false, true);
        scenario.SetupUpdateFailure(
            new InvalidOperationException("Persistence failed."),
            static (comment, expectedRevision) =>
                comment.Revision = expectedRevision + 1);
        scenario.SetupRecovery(static () => null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.Handler.HandleAsync(
                scenario.Command,
                scenario.OperationToken));

        scenario.VerifyAll();
        scenario.VerifyReservationWasNotReleased();
        scenario.VerifyReservationWasNotFinalized();
    }

    [Fact]
    public async Task HandleAsync_WhenCommittedReplacementIsCancelled_ShouldRecoverWithIndependentToken()
    {
        using UpdateWithImageScenario scenario = new UpdateWithImageScenario(true);
        Comment? committed = null;
        scenario.SetupUpdateFailure(
            new OperationCanceledException(scenario.OperationToken),
            (comment, expectedRevision) =>
            {
                comment.Revision = expectedRevision + 1;
                committed = comment;
            });
        scenario.SetupRecovery(() => committed);
        scenario.SetupFinalization();

        ApplicationResult<CommentResult> result =
            await scenario.Handler.HandleAsync(
                scenario.Command,
                scenario.OperationToken);

        Assert.True(result.IsSuccess);
        scenario.Comments.Verify(repository => repository.GetByIdAsync(
            UpdateWithImageScenario.CommentId,
            CancellationToken.None), Times.Once);
        scenario.VerifyAll();
        scenario.VerifyReservationWasNotReleased();
    }


}
