namespace AmusementPark.Application.Features.ContextualBlocks.Results;

public sealed class ContextualBlockJsonExportResult
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/json";

    public string Json { get; init; } = string.Empty;
}
