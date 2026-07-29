using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Results;

namespace AmusementPark.Application.Features.Comments.Commands;

public sealed record UploadCommentImageCommand(
    string ActorUserId,
    FilePayload File) : ICommand<ApplicationResult<UploadedImageResult>>;

public sealed record DeleteCommentDraftImageCommand(
    string ActorUserId,
    string ImageId) : ICommand<ApplicationResult>;
