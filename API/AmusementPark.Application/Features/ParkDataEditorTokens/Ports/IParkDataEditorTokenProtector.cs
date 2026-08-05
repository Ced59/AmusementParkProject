using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Ports;

public interface IParkDataEditorTokenProtector
{
    ParkDataEditorTokenMaterial Create(string tokenId);

    bool TryReadTokenId(string plainTextToken, out string tokenId);

    bool Verify(string plainTextToken, ParkDataEditorAccessToken token);
}

public sealed record ParkDataEditorTokenMaterial(
    string PlainTextToken,
    string TokenHash,
    string DisplayPrefix);
