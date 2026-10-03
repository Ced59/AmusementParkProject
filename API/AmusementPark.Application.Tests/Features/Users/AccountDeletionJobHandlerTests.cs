using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.Users.Models;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class AccountDeletionJobHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithInvalidPayload_ShouldDeadLetter()
    {
        AccountDeletionCoordinator coordinator = CreateUnusedCoordinator();
        Mock<IAccountDeletionOperationRepository> operations =
            new Mock<IAccountDeletionOperationRepository>(MockBehavior.Strict);
        AccountDeletionJobHandler handler = new AccountDeletionJobHandler(
            coordinator,
            operations.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(JsonSerializer.SerializeToElement(new { invalid = true })),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.DeadLetter, result.Outcome);
        Assert.Equal("account-deletion.invalid-payload", result.ErrorCode);
        operations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenOperationAlreadyCompleted_ShouldSucceedIdempotently()
    {
        AccountDeletionCoordinator coordinator = CreateUnusedCoordinator();
        Mock<IAccountDeletionOperationRepository> operations =
            new Mock<IAccountDeletionOperationRepository>(MockBehavior.Strict);
        operations.Setup(value => value.GetAsync("operation-1", CancellationToken.None))
            .ReturnsAsync((AccountDeletionOperation?)null);
        AccountDeletionJobHandler handler = new AccountDeletionJobHandler(
            coordinator,
            operations.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(CreatePayload()),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        operations.VerifyAll();
    }

    private static AccountDeletionCoordinator CreateUnusedCoordinator()
    {
        return new AccountDeletionCoordinator(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static JsonElement CreatePayload()
    {
        return JsonSerializer.SerializeToElement(
            new AccountDeletionJobPayload("operation-1"));
    }

    private static DurableBackgroundJobExecutionContext CreateContext(JsonElement payload)
    {
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            AccountDeletionJob.PayloadVersion,
            payload,
            null,
            1,
            null);
    }
}
