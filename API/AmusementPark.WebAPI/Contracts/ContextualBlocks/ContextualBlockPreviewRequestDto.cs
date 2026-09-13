using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ContextualBlocks;

public sealed class ContextualBlockPreviewRequestDto
{
    public JsonElement Document { get; set; }
}
