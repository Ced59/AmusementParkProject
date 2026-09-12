using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Contracts;

public sealed record SocialPublisherLinkReconciliationResult(
    bool IsSuccess,
    bool IsFound,
    bool IsAmbiguous,
    string? ExternalPostId,
    string? ExternalPostUrl,
    string? FailureCode,
    string? FailureMessage);

