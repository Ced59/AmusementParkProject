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

public sealed class GetSeoSitemapSettingsQueryHandler : IQueryHandler<GetSeoSitemapSettingsQuery, ApplicationResult<SeoSitemapSettings>>
{
    private readonly ISeoSitemapSettingsRepository settingsRepository;

    public GetSeoSitemapSettingsQueryHandler(ISeoSitemapSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository;
    }

    public async Task<ApplicationResult<SeoSitemapSettings>> HandleAsync(GetSeoSitemapSettingsQuery query, CancellationToken cancellationToken = default)
    {
        SeoSitemapSettings settings = await this.settingsRepository.GetAsync(cancellationToken);
        return ApplicationResult<SeoSitemapSettings>.Success(settings);
    }
}
