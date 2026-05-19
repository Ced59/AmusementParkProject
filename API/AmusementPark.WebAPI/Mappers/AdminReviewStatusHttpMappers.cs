using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Mappers HTTP du statut de revue admin.
/// </summary>
internal static class AdminReviewStatusHttpMappers
{
    public static AdminReviewStatus ToDomain(this AdminReviewStatusDto value)
    {
        return value switch
        {
            AdminReviewStatusDto.Validated => AdminReviewStatus.Validated,
            AdminReviewStatusDto.Ready => AdminReviewStatus.Validated,
            AdminReviewStatusDto.ToProcessLater => AdminReviewStatus.ToProcessLater,
            AdminReviewStatusDto.NotRelevant => AdminReviewStatus.NotRelevant,
            _ => AdminReviewStatus.ToReview,
        };
    }

    public static AdminReviewStatus? ToOptionalDomain(this AdminReviewStatusDto? value)
    {
        return value.HasValue ? value.Value.ToDomain() : null;
    }

    public static AdminReviewStatusDto ToHttp(this AdminReviewStatus value)
    {
        return value switch
        {
            AdminReviewStatus.Validated => AdminReviewStatusDto.Validated,
            AdminReviewStatus.ToProcessLater => AdminReviewStatusDto.ToProcessLater,
            AdminReviewStatus.NotRelevant => AdminReviewStatusDto.NotRelevant,
            _ => AdminReviewStatusDto.ToReview,
        };
    }

    public static BulkAdministrationUpdateResultDto ToHttp(this BulkAdministrationUpdateResult value)
    {
        return new BulkAdministrationUpdateResultDto
        {
            RequestedCount = value.RequestedCount,
            UpdatedCount = value.UpdatedCount,
        };
    }
}
