using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Services.Authentication;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Authentication;

public sealed class ParkDataEditorTokenProtectorTests
{
    [Fact]
    public void Create_ShouldProduceParseableVerifiableOpaqueToken()
    {
        ParkDataEditorTokenProtector protector = new ParkDataEditorTokenProtector();
        string tokenId = Guid.NewGuid().ToString("N");

        AmusementPark.Application.Features.ParkDataEditorTokens.Ports.ParkDataEditorTokenMaterial material =
            protector.Create(tokenId);
        ParkDataEditorAccessToken token = new ParkDataEditorAccessToken
        {
            Id = tokenId,
            TokenHash = material.TokenHash,
        };

        Assert.True(protector.TryReadTokenId(material.PlainTextToken, out string parsedId));
        Assert.Equal(tokenId, parsedId);
        Assert.True(protector.Verify(material.PlainTextToken, token));
        Assert.False(protector.Verify(material.PlainTextToken + "tampered", token));
        Assert.DoesNotContain(material.PlainTextToken, material.TokenHash, StringComparison.Ordinal);
    }
}
