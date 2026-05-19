using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Helpers de mapping HTTP pour le statut de traitement administratif partagé par les listes admin.
/// </summary>
internal static class AdminReviewStatusHttpMappers
{
    public static AdminReviewStatus ToDomain(this AdminReviewStatusDto value)
    {
        return value switch
        {
            AdminReviewStatusDto.ToProcessLater => AdminReviewStatus.ToProcessLater,
            _ => AdminReviewStatus.Ready,
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
            AdminReviewStatus.ToProcessLater => AdminReviewStatusDto.ToProcessLater,
            _ => AdminReviewStatusDto.Ready,
        };
    }
}
