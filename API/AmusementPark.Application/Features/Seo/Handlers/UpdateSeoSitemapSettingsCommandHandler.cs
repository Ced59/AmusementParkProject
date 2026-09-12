using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Commands;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.Application.Features.Seo.Services;

namespace AmusementPark.Application.Features.Seo.Handlers;

public sealed class UpdateSeoSitemapSettingsCommandHandler : ICommandHandler<UpdateSeoSitemapSettingsCommand, ApplicationResult<SeoSitemapSettings>>
{
    private readonly ISeoSitemapSettingsRepository settingsRepository;

    public UpdateSeoSitemapSettingsCommandHandler(ISeoSitemapSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository;
    }

    public async Task<ApplicationResult<SeoSitemapSettings>> HandleAsync(UpdateSeoSitemapSettingsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        string normalizedKey = Normalize(command.IndexNowKey);
        string normalizedKeyLocation = Normalize(command.IndexNowKeyLocation);
        if (command.IsIndexNowEnabled && string.IsNullOrWhiteSpace(normalizedKey))
        {
            return ApplicationResult<SeoSitemapSettings>.Failure(ApplicationErrors.Required("indexNowKey"));
        }

        SeoSitemapSettings settings = new SeoSitemapSettings
        {
            IsIndexNowEnabled = command.IsIndexNowEnabled,
            SubmitToIndexNowAfterManualGeneration = false,
            SubmitToIndexNowAfterAutomaticGeneration = false,
            IndexNowKey = normalizedKey,
            IndexNowKeyLocation = normalizedKeyLocation,
            IndexNowEndpoints = NormalizeEndpoints(command.IndexNowEndpoints),
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await this.settingsRepository.SaveAsync(settings, cancellationToken);
        return ApplicationResult<SeoSitemapSettings>.Success(settings);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static IReadOnlyCollection<string> NormalizeEndpoints(IReadOnlyCollection<string> endpoints)
    {
        List<string> normalized = endpoints
            .Where(static endpoint => !string.IsNullOrWhiteSpace(endpoint))
            .Select(static endpoint => endpoint.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            normalized.Add("https://api.indexnow.org/indexnow");
            normalized.Add("https://www.bing.com/indexnow");
        }

        return normalized;
    }
}
