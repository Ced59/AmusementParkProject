using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserNotificationRepositoryTests
{
    [Fact]
    public void IsDuplicateOnlyFailure_WithNoWriteError_ShouldPropagateFailure()
    {
        bool result = UserNotificationRepository.IsDuplicateOnlyFailure(
            Array.Empty<ServerErrorCategory>(),
            hasWriteConcernError: false);

        Assert.False(result);
    }

    [Fact]
    public void IsDuplicateOnlyFailure_WithWriteConcernError_ShouldPropagateFailure()
    {
        bool result = UserNotificationRepository.IsDuplicateOnlyFailure(
            new[] { ServerErrorCategory.DuplicateKey },
            hasWriteConcernError: true);

        Assert.False(result);
    }

    [Fact]
    public void IsDuplicateOnlyFailure_WithMixedWriteErrors_ShouldPropagateFailure()
    {
        bool result = UserNotificationRepository.IsDuplicateOnlyFailure(
            new[] { ServerErrorCategory.DuplicateKey, ServerErrorCategory.Uncategorized },
            hasWriteConcernError: false);

        Assert.False(result);
    }

    [Fact]
    public void IsDuplicateOnlyFailure_WithDuplicateWriteErrors_ShouldAllowIdempotency()
    {
        bool result = UserNotificationRepository.IsDuplicateOnlyFailure(
            new[] { ServerErrorCategory.DuplicateKey },
            hasWriteConcernError: false);

        Assert.True(result);
    }
}
