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

public sealed class GenerateSitemapCommandHandler : ICommandHandler<GenerateSitemapCommand, ApplicationResult<SitemapGenerationResult>>
{
    private readonly SeoSitemapGenerationOrchestrator orchestrator;

    public GenerateSitemapCommandHandler(SeoSitemapGenerationOrchestrator orchestrator)
    {
        this.orchestrator = orchestrator;
    }

    public async Task<ApplicationResult<SitemapGenerationResult>> HandleAsync(GenerateSitemapCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        SitemapGenerationResult result = await this.orchestrator.GenerateAsync(
            command.PublicBaseUrl,
            new SitemapGenerationContext
            {
                SupportedLanguages = command.SupportedLanguages,
            },
            command.Trigger,
            command.TriggeredByUserId,
            command.TriggeredByUserEmail,
            cancellationToken);

        return ApplicationResult<SitemapGenerationResult>.Success(result);
    }

}
