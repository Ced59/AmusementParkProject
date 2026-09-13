namespace AmusementPark.WebAPI.Contracts.Seo;

public sealed class SeoSitemapGenerationHistoryDto : SeoSitemapGenerationResultDto
{
    public string? TriggeredByUserId { get; init; }

    public string? TriggeredByUserEmail { get; init; }
}
