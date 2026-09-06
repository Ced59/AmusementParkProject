using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Contracts;

public sealed record ImageMutationPrecondition(
    ImageOwnerType OwnerType,
    string? OwnerId,
    ImageCategory Category,
    bool IsCurrent,
    DateTime? UpdatedAtUtc = null);
