namespace AmusementPark.Application.Features.SocialShare.Contracts;

public sealed record SocialShareEventCaptureResult(bool Accepted, DateTime? OccurredAtUtc);
