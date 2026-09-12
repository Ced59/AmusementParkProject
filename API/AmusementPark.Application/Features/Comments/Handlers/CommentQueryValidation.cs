using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Queries;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Comments.Handlers;

internal static class CommentQueryValidation
{
    public static async Task<ApplicationResult<CommentTargetMetadataResult>> ResolveTargetAsync(
        CommentTargetType targetType,
        string targetId,
        bool includeHidden,
        CommentTargetResolver targetResolver,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(targetType))
        {
            return ApplicationResult<CommentTargetMetadataResult>.Failure(CommentApplicationErrors.InvalidTargetType());
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            return ApplicationResult<CommentTargetMetadataResult>.Failure(ApplicationErrors.Required(nameof(targetId)));
        }

        CommentTargetMetadataResult? target = await targetResolver.ResolveAsync(
            targetType,
            targetId.Trim(),
            includeHidden,
            cancellationToken);
        return target is null
            ? ApplicationResult<CommentTargetMetadataResult>.Failure(CommentApplicationErrors.TargetNotFound())
            : ApplicationResult<CommentTargetMetadataResult>.Success(target);
    }
}
