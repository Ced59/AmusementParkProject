using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

public sealed class GetShareModerationReportsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenMongoOffsetWouldOverflow_ShouldRejectWithoutReading()
    {
        Mock<IShareModerationReportRepository> repository =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        GetShareModerationReportsQueryHandler handler =
            new GetShareModerationReportsQueryHandler(repository.Object);

        ApplicationResult<PagedResult<ShareModerationReportResult>> result =
            await handler.HandleAsync(
                new GetShareModerationReportsQuery(
                    new ShareModerationReportSearchCriteria(
                        new PagedQuery(int.MaxValue, 100))),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-moderation.search-invalid");
        repository.VerifyNoOtherCalls();
    }
}
