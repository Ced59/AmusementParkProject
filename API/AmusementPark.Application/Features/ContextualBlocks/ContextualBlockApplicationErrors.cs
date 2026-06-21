using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ContextualBlocks;

internal static class ContextualBlockApplicationErrors
{
    public static ApplicationError UnsupportedBlockType(string blockType)
    {
        return ApplicationError.Validation(
            "contextual-block.unsupported-block-type",
            $"Le type de bloc '{blockType}' n'est pas exportable en JSON borne.");
    }
}
