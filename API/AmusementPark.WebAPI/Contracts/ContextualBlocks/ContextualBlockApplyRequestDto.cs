using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ContextualBlocks;

public sealed class ContextualBlockApplyRequestDto
{
    public JsonElement Document { get; set; }
}
