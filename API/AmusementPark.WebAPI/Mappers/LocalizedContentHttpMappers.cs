using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.WebAPI.Contracts.LocalizedContent;

namespace AmusementPark.WebAPI.Mappers;

internal static class LocalizedContentHttpMappers
{
    public static LocalizedContentTargetDto ToHttp(this LocalizedContentTargetResult value)
    {
        return new LocalizedContentTargetDto
        {
            EntityType = value.EntityType,
            EntityId = value.EntityId,
            Label = value.Label,
            Context = value.Context,
            SupportedFields = value.SupportedFields,
        };
    }

    public static LocalizedContentApplyResultDto ToHttp(this LocalizedContentApplyResult value)
    {
        return new LocalizedContentApplyResultDto
        {
            EntityType = value.EntityType,
            EntityId = value.EntityId,
            UpdatedFields = value.UpdatedFields,
            UpdatedLocalizedValueCount = value.UpdatedLocalizedValueCount,
        };
    }
}
