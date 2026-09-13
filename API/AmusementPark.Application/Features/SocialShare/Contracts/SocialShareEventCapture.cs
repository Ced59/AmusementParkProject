namespace AmusementPark.Application.Features.SocialShare.Contracts;

public sealed record SocialShareEventCapture(
    string? TargetType,
    string? TargetId,
    string? TargetTitle,
    string? Url,
    string? LanguageCode,
    string? Channel,
    string? UserId);
