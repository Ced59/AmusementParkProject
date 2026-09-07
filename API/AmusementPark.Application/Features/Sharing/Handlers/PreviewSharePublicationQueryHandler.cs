using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class PreviewSharePublicationQueryHandler
    : IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>
{
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationPreviewBuilder> builders;
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSourceDescriptor> sources;
    private readonly ISharePublicationRepository repository;
    private readonly ISharePublicationPreviewApprovalProtector approvalProtector;

    public PreviewSharePublicationQueryHandler(
        IEnumerable<ISharePublicationPreviewBuilder> builders,
        IEnumerable<ISharePublicationSourceDescriptor> sources,
        ISharePublicationRepository repository,
        ISharePublicationPreviewApprovalProtector approvalProtector)
    {
        ArgumentNullException.ThrowIfNull(builders);
        ArgumentNullException.ThrowIfNull(sources);
        this.builders = builders.ToDictionary(static builder => builder.PublicationType);
        this.sources = sources.ToDictionary(static source => source.PublicationType);
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.approvalProtector = approvalProtector
            ?? throw new ArgumentNullException(nameof(approvalProtector));
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

        if (!this.builders.TryGetValue(query.PublicationType, out ISharePublicationPreviewBuilder? builder)
            || !this.sources.TryGetValue(
                query.PublicationType,
                out ISharePublicationSourceDescriptor? source))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.PreviewTypeNotAvailable());
        }

        ApplicationResult<bool> policyValidation = source.ValidatePolicyForPublication(contentPolicy);
        if (!policyValidation.IsSuccess)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(policyValidation.Errors);
        }

        string ownerUserId = query.OwnerUserId.Trim();
        ApplicationResult<string> scopeResult = source.ResolveSourceScopeKey(
            ownerUserId,
            query.SourceId);
        if (!scopeResult.IsSuccess || scopeResult.Value is null)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(scopeResult.Errors);
        }

        SharePublication? publicationBefore = await this.repository.GetOwnedBySourceAsync(
            ownerUserId,
            query.PublicationType,
            scopeResult.Value,
            cancellationToken);
        SharePublicationApprovalState publicationState =
            SharePublicationApprovalState.From(publicationBefore);

        ApplicationResult<SharePublicationPreviewResult> previewResult;
        if (query.PublicationType == SharePublicationType.VisitRecap)
        {
            if (builder is not IVisitRecapSharePreviewBuilder visitRecapBuilder
                || string.IsNullOrWhiteSpace(query.SourceId))
            {
                return ApplicationResult<SharePublicationPreviewResult>.Failure(
                    SharingApplicationErrors.PreviewTypeNotAvailable());
            }

            previewResult = await visitRecapBuilder.BuildAsync(
                ownerUserId,
                query.SourceId.Trim(),
                contentPolicy,
                query.VisitRecap,
                cancellationToken);
        }
        else
        {
            if (query.VisitRecap is not null)
            {
                return ApplicationResult<SharePublicationPreviewResult>.Failure(
                    SharingApplicationErrors.InvalidSource());
            }

            previewResult = await builder.BuildAsync(
                ownerUserId,
                query.SourceId,
                contentPolicy,
                cancellationToken);
        }
        if (!previewResult.IsSuccess || previewResult.Value is null)
        {
            return previewResult;
        }

        SharePublication? publicationAfter = await this.repository.GetOwnedBySourceAsync(
            ownerUserId,
            query.PublicationType,
            scopeResult.Value,
            cancellationToken);
        if (!publicationState.Matches(publicationAfter))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.PublicationChangedConcurrently());
        }

        string approvalToken = this.approvalProtector.CreateToken(
            ownerUserId,
            query.PublicationType,
            scopeResult.Value,
            previewResult.Value.SourceVersion,
            publicationState,
            contentPolicy,
            previewResult.Value.ContentFingerprint);
        return ApplicationResult<SharePublicationPreviewResult>.Success(
            previewResult.Value with
            {
                PublicationType = query.PublicationType,
                PolicySchemaVersion = contentPolicy.SchemaVersion,
                DatePrecision = contentPolicy.DatePrecision,
                IncludedFields = contentPolicy.IncludedFields,
                ApprovalToken = approvalToken,
            });
    }
}
