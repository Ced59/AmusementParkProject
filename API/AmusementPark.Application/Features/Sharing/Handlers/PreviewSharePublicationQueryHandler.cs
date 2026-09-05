using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class PreviewSharePublicationQueryHandler
    : IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>
{
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationPreviewBuilder> builders;

    public PreviewSharePublicationQueryHandler(
        IEnumerable<ISharePublicationPreviewBuilder> builders)
    {
        ArgumentNullException.ThrowIfNull(builders);
        this.builders = builders.ToDictionary(static builder => builder.PublicationType);
    }

    public async Task<ApplicationResult<SharePublicationPreviewResult>> HandleAsync(
        PreviewSharePublicationQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.OwnerUserId))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                ApplicationErrors.Required(nameof(query.OwnerUserId)));
        }

        if (!Enum.IsDefined(query.PublicationType))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.InvalidPublicationType());
        }

        ShareContentPolicy contentPolicy;
        try
        {
            contentPolicy = ShareContentPolicy.Create(
                query.PublicationType,
                query.DatePrecision,
                query.IncludedFields);
        }
        catch (ShareContentPolicyValidationException exception)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.InvalidContentPolicy(exception.ErrorCode));
        }

        if (!this.builders.TryGetValue(query.PublicationType, out ISharePublicationPreviewBuilder? builder))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.PreviewTypeNotAvailable());
        }

        return await builder.BuildAsync(
            query.OwnerUserId.Trim(),
            query.SourceId,
            contentPolicy,
            cancellationToken);
    }
}
