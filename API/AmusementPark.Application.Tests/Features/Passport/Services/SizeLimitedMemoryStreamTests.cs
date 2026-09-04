using AmusementPark.Application.Features.Passport.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class SizeLimitedMemoryStreamTests
{
    [Fact]
    public void Write_StopsBeforeAllocatingPastConfiguredLimit()
    {
        using SizeLimitedMemoryStream stream = new SizeLimitedMemoryStream(4);
        stream.Write(new byte[] { 1, 2, 3 });

        Assert.Throws<PassportExportSizeLimitException>(() =>
            stream.Write(new byte[] { 4, 5 }));
        Assert.Equal(3, stream.Length);
    }
}
