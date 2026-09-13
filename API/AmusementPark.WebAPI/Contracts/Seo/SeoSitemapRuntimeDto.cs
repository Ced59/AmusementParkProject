namespace AmusementPark.WebAPI.Contracts.Seo;

public sealed class SeoSitemapRuntimeDto
{
    public string Status { get; init; } = string.Empty;

    public string CurrentStep { get; init; } = string.Empty;

    public int ProgressPercentage { get; init; }

    public DateTime? StartedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public string? Message { get; init; }
}
