namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// État runtime exposé au panneau admin.
/// </summary>
public sealed class SitemapRuntimeState
{
    public SitemapGenerationStatus Status { get; init; } = SitemapGenerationStatus.Idle;

    public string CurrentStep { get; init; } = "idle";

    public int ProgressPercentage { get; init; }

    public DateTime? StartedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public string? Message { get; init; }
}
