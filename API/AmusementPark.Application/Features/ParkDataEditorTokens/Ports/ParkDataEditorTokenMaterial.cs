using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Ports;

public sealed record ParkDataEditorTokenMaterial(
    string PlainTextToken,
    string TokenHash,
    string DisplayPrefix);
