using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Contracts;

public sealed record SocialPublisherPostSnapshotResult(
    bool IsSuccess,
    bool Exists,
    string? Message,
    string? ExternalPostUrl,
    string? FailureCode,
    string? FailureMessage);

