using System.Text.Json;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.LocalizedContent;

public sealed class ApplyLocalizedContentJsonRequestDto
{
    public JsonElement Json { get; init; }
}
