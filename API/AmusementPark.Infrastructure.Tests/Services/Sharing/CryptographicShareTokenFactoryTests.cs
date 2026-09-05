using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Services.Sharing;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Sharing;

public sealed class CryptographicShareTokenFactoryTests
{
    [Fact]
    public void Generate_ShouldReturnCanonicalDistinctTokensWith256BitsOfEntropy()
    {
        CryptographicShareTokenFactory factory = new CryptographicShareTokenFactory();
        HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < 256; index++)
        {
            ShareToken token = factory.Generate();

            Assert.Equal(ShareToken.EncodedLength, token.Value.Length);
            Assert.True(ShareToken.TryParse(token.Value, out ShareToken reparsed));
            Assert.Equal(token, reparsed);
            Assert.DoesNotContain('=', token.Value);
            Assert.True(values.Add(token.Value));
        }
    }
}
