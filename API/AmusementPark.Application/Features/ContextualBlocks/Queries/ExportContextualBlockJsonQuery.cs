using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Results;

namespace AmusementPark.Application.Features.ContextualBlocks.Queries;

public sealed record ExportContextualBlockJsonQuery(string BlockType, string EntityId)
    : IQuery<ApplicationResult<ContextualBlockJsonExportResult>>;
