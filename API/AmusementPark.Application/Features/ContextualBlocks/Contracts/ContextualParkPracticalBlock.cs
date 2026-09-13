using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Contracts;

public sealed class ContextualParkPracticalBlock
{
    public string ParkId { get; init; } = string.Empty;

    public string? CountryCode { get; init; }

    public string? City { get; init; }

    public string? Street { get; init; }

    public string? PostalCode { get; init; }

    public string? WebsiteUrl { get; init; }

    public string? FounderId { get; init; }

    public string? OperatorId { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}
