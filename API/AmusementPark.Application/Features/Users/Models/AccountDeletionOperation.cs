namespace AmusementPark.Application.Features.Users.Models;

public sealed record AccountDeletionOperation(
    string Id,
    string UserId,
    DateTime CreatedAtUtc);
