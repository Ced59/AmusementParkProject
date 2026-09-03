using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class PassportRideOccurrenceCursorCodecTests
{
    [Fact]
    public void EncodeAndDecode_ShouldRoundTripTheStableOrderTuple()
    {
        RideOccurrenceListCursor source = new RideOccurrenceListCursor(
            1536,
            new DateTime(2026, 9, 3, 10, 30, 0, DateTimeKind.Utc),
            RideOccurrenceId.Parse("occurrence-1"));

        string encoded = PassportRideOccurrenceCursorCodec.Encode(source);
        bool decoded = PassportRideOccurrenceCursorCodec.TryDecode(
            encoded,
            out RideOccurrenceListCursor? result);

        Assert.True(decoded);
        Assert.Equal(source, result);
    }

    [Theory]
    [InlineData("invalid!")]
    [InlineData("e30")]
    public void TryDecode_WithInvalidPayload_ShouldFail(string value)
    {
        Assert.False(PassportRideOccurrenceCursorCodec.TryDecode(
            value,
            out RideOccurrenceListCursor? cursor));
        Assert.Null(cursor);
    }
}
