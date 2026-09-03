using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class PassportVisitCursorCodecTests
{
    [Fact]
    public void EncodeAndDecode_ShouldRoundTripAStableOpaqueCursor()
    {
        UserVisitListCursor source = new UserVisitListCursor(
            VisitDate.ForMonth(1998, 7, isApproximate: true),
            new DateTime(2026, 9, 3, 10, 30, 0, DateTimeKind.Utc),
            VisitId.Parse("visit-1"));

        string encoded = PassportVisitCursorCodec.Encode(source);
        bool decoded = PassportVisitCursorCodec.TryDecode(encoded, out UserVisitListCursor? result);

        Assert.True(decoded);
        Assert.Equal(source, result);
        Assert.DoesNotContain("visit-1", encoded, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not-base64!")]
    [InlineData("eyJWZXJzaW9uIjo5OX0")]
    public void TryDecode_WhenCursorIsMalformedOrUnsupported_ShouldRejectIt(string value)
    {
        Assert.False(PassportVisitCursorCodec.TryDecode(value, out UserVisitListCursor? cursor));
        Assert.Null(cursor);
    }

    [Fact]
    public void TryDecode_WhenCursorIsAbsent_ShouldRepresentTheFirstPage()
    {
        Assert.True(PassportVisitCursorCodec.TryDecode(null, out UserVisitListCursor? cursor));
        Assert.Null(cursor);
    }
}
