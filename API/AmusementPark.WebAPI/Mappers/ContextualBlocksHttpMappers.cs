using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.WebAPI.Contracts.ContextualBlocks;

namespace AmusementPark.WebAPI.Mappers;

public static class ContextualBlocksHttpMappers
{
    public static ContextualBlockPreviewResultDto ToHttp(this ContextualBlockPreviewResult result)
    {
        return new ContextualBlockPreviewResultDto
        {
            OperationId = result.OperationId,
            BlockType = result.BlockType,
            IsApplied = result.IsApplied,
            CanApply = result.CanApply,
            PreviewedAtUtc = result.PreviewedAtUtc,
            Target = result.Target.ToHttp(),
            Counts = result.Counts.ToHttp(),
            Changes = result.Changes.Select(static change => change.ToHttp()).ToList(),
            Warnings = result.Warnings.ToList(),
            Errors = result.Errors.ToList(),
        };
    }

    private static ContextualBlockPreviewTargetDto ToHttp(this ContextualBlockPreviewTarget target)
    {
        return new ContextualBlockPreviewTargetDto
        {
            EntityType = target.EntityType,
            EntityId = target.EntityId,
            DisplayName = target.DisplayName,
        };
    }

    private static ContextualBlockPreviewCountsDto ToHttp(this ContextualBlockPreviewCounts counts)
    {
        return new ContextualBlockPreviewCountsDto
        {
            Created = counts.Created,
            Updated = counts.Updated,
            Deleted = counts.Deleted,
            Unchanged = counts.Unchanged,
            Warnings = counts.Warnings,
            Errors = counts.Errors,
        };
    }

    private static ContextualBlockPreviewChangeDto ToHttp(this ContextualBlockPreviewChange change)
    {
        return new ContextualBlockPreviewChangeDto
        {
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            DisplayName = change.DisplayName,
            Field = change.Field,
            LanguageCode = change.LanguageCode,
            ChangeType = change.ChangeType,
            OldValue = change.OldValue,
            NewValue = change.NewValue,
        };
    }
}
