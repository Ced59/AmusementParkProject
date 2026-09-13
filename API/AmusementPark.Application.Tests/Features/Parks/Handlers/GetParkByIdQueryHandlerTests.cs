using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class GetParkByIdQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParkIdIsBlank_ShouldFailWithoutCallingRepository()
    {
        GetParkByIdQueryHandlerTestsFakeParkRepository repository = new GetParkByIdQueryHandlerTestsFakeParkRepository();
        GetParkByIdQueryHandler handler = new GetParkByIdQueryHandler(repository);

        ApplicationResult<Park> result = await handler.HandleAsync(new GetParkByIdQuery("   "));

        Assert.False(result.IsSuccess);
        Assert.Empty(repository.GetByIdCalls);
        Assert.Contains(result.Errors, static error => error.Code == "park.not-found");
    }

    [Fact]
    public async Task HandleAsync_WhenParkExists_ShouldTrimIdAndReturnPark()
    {
        Park park = CreatePark("park-1");
        GetParkByIdQueryHandlerTestsFakeParkRepository repository = new GetParkByIdQueryHandlerTestsFakeParkRepository
        {
            ParkById = park
        };
        GetParkByIdQueryHandler handler = new GetParkByIdQueryHandler(repository);

        ApplicationResult<Park> result = await handler.HandleAsync(new GetParkByIdQuery(" park-1 ", true));

        Assert.True(result.IsSuccess);
        Assert.Same(park, result.Value);
        Assert.Equal(new[] { new GetByIdCall("park-1", true) }, repository.GetByIdCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenParkDoesNotExist_ShouldFail()
    {
        GetParkByIdQueryHandlerTestsFakeParkRepository repository = new GetParkByIdQueryHandlerTestsFakeParkRepository();
        GetParkByIdQueryHandler handler = new GetParkByIdQueryHandler(repository);

        ApplicationResult<Park> result = await handler.HandleAsync(new GetParkByIdQuery("park-404"));

        Assert.False(result.IsSuccess);
        Assert.Equal(new[] { new GetByIdCall("park-404", false) }, repository.GetByIdCalls);
    }

    private static Park CreatePark(string id)
    {
        Park park = new Park
        {
            Id = id,
            Name = "Test park",
            IsVisible = true
        };
        park.SetPosition(48.8, 2.3);
        return park;
    }




}
