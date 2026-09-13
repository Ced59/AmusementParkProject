using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.OutputCaching;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

internal sealed class InputDto
{
    public string ParkId { get; init; } = string.Empty;

    public bool IsVisible { get; init; }

    public IReadOnlyCollection<string> Ids { get; init; } = Array.Empty<string>();
}
