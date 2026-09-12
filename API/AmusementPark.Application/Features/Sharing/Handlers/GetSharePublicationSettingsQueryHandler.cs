using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetSharePublicationSettingsQueryHandler
    : IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>>
{
    private readonly ISharePublicationRepository repository;
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSourceDescriptor> sources;

    public GetSharePublicationSettingsQueryHandler(
        ISharePublicationRepository repository,
        IEnumerable<ISharePublicationSourceDescriptor> sources)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources.ToDictionary(static source => source.PublicationType);
    }

    public async Task<ApplicationResult<SharePublicationSettingsResult>> HandleAsync(
        GetSharePublicationSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        if (!this.sources.TryGetValue(
                query.PublicationType,
                out ISharePublicationSourceDescriptor? source))
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.InvalidPublicationType());
        }

        ApplicationResult<string> scopeResult = source.ResolveSourceScopeKey(
            query.UserId,
            query.SourceId);
        if (!scopeResult.IsSuccess || scopeResult.Value is null)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(scopeResult.Errors);
        }

        SharePublication? publication = await this.repository.GetOwnedBySourceAsync(
            query.UserId.Trim(),
            query.PublicationType,
            scopeResult.Value,
            cancellationToken);
        bool isSourceCurrent = true;
        if (publication?.IsResolvable == true)
        {
            ApplicationResult<long> currentVersion = await source.GetCurrentSourceVersionAsync(
                new SharePublicationSourceVersionRequest(
                    scopeResult.Value,
                    publication.ContentPolicy,
                    publication.Id,
                    publication.PublicationVersion),
                cancellationToken);
            isSourceCurrent = currentVersion.IsSuccess
                && currentVersion.Value == publication.SourceVersion;
        }

        return ApplicationResult<SharePublicationSettingsResult>.Success(
            SharePublicationSettingsMapper.ToResult(publication, isSourceCurrent));
    }
}
