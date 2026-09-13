namespace AmusementPark.Application.Features.SocialShare.Contracts;

public sealed record SocialShareTopTarget(
    string TargetType,
    string? TargetId,
    string? TargetTitle,
    string Url,
    long Count);
