using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Commands;

public sealed record GenerateSitemapCommand(
    string PublicBaseUrl,
    IReadOnlyCollection<string> SupportedLanguages,
    SitemapGenerationTrigger Trigger,
    bool SubmitToIndexNow,
    string? TriggeredByUserId,
    string? TriggeredByUserEmail)
    : ICommand<ApplicationResult<SitemapGenerationResult>>;

public sealed record UpdateSeoSitemapSettingsCommand(
    bool IsIndexNowEnabled,
    bool SubmitToIndexNowAfterManualGeneration,
    bool SubmitToIndexNowAfterAutomaticGeneration,
    string? IndexNowKey,
    string? IndexNowKeyLocation,
    IReadOnlyCollection<string> IndexNowEndpoints)
    : ICommand<ApplicationResult<SeoSitemapSettings>>;
