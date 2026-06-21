using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Results;

namespace AmusementPark.Application.Features.ContextualBlocks.Commands;

public sealed record ApplyContextualBlockJsonCommand(string BlockType, string EntityId, JsonElement Document)
    : ICommand<ApplicationResult<ContextualBlockPreviewResult>>;
