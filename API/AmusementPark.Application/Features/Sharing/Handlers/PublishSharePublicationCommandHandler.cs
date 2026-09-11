using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class PublishSharePublicationCommandHandler
    : ICommandHandler<PublishSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>>
{
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSourceDescriptor> sources;
    private readonly SharePublicationPublisher publisher;
    private readonly ISharePublicationRepository repository;
    private readonly ISharePublicationPreviewApprovalProtector approvalProtector;

    public PublishSharePublicationCommandHandler(
        IEnumerable<ISharePublicationSourceDescriptor> sources,
        SharePublicationPublisher publisher,
        ISharePublicationRepository repository,
        ISharePublicationPreviewApprovalProtector approvalProtector)
    {
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources.ToDictionary(static source => source.PublicationType);
        this.publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.approvalProtector = approvalProtector
            ?? throw new ArgumentNullException(nameof(approvalProtector));
    }

    public async Task<ApplicationResult<SharePublicationSettingsResult>> HandleAsync(
        PublishSharePublicationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                ApplicationErrors.Required(nameof(command.UserId)));
        }

        if (!this.sources.TryGetValue(
                command.PublicationType,
                out ISharePublicationSourceDescriptor? source))
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.InvalidPublicationType());
        }

        string ownerUserId = command.UserId.Trim();
        ApplicationResult<string> scopeResult = source.ResolveSourceScopeKey(
            ownerUserId,
            command.SourceId);
        if (!scopeResult.IsSuccess || scopeResult.Value is null)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(scopeResult.Errors);
        }

        ShareContentPolicy contentPolicy;
        try
        {
            contentPolicy = ShareContentPolicy.Restore(
                command.PublicationType,
                command.ApprovedPolicySchemaVersion,
                command.ApprovedDatePrecision,
                command.ApprovedIncludedFields);
        }
        catch (ShareContentPolicyValidationException exception)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.InvalidContentPolicy(exception.ErrorCode));
        }

        ApplicationResult<bool> policyResult = source.ValidatePolicyForPublication(contentPolicy);
        if (!policyResult.IsSuccess)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(policyResult.Errors);
        }

        SharePublication? currentPublication = await this.repository.GetOwnedBySourceAsync(
            ownerUserId,
            command.PublicationType,
            scopeResult.Value,
            cancellationToken);
        SharePublicationApprovalState publicationState =
            SharePublicationApprovalState.From(currentPublication);

        string contentFingerprint = string.Empty;
        VisitRecapShareInput? normalizedVisitRecap = null;
        YearRecapShareInput? normalizedYearRecap = null;
        if (command.PublicationType == SharePublicationType.VisitRecap)
        {
            if (command.YearRecap is not null)
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    SharingApplicationErrors.InvalidSource());
            }

            ApplicationResult<VisitRecapShareInput> inputResult =
                VisitRecapShareInputNormalizer.Normalize(command.VisitRecap, contentPolicy);
            if (!inputResult.IsSuccess || inputResult.Value is null)
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(inputResult.Errors);
            }

            normalizedVisitRecap = inputResult.Value;
            contentFingerprint = VisitRecapShareInputNormalizer.CreateFingerprint(
                normalizedVisitRecap);
        }
        else if (command.PublicationType == SharePublicationType.YearRecap)
        {
            if (command.VisitRecap is not null)
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    SharingApplicationErrors.InvalidSource());
            }

            ApplicationResult<YearRecapShareInput> inputResult =
                YearRecapShareInputNormalizer.Normalize(command.YearRecap, contentPolicy);
            if (!inputResult.IsSuccess || inputResult.Value is null)
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(inputResult.Errors);
            }

            normalizedYearRecap = inputResult.Value;
            contentFingerprint = YearRecapShareInputNormalizer.CreateFingerprint(
                normalizedYearRecap);
        }
        else if (command.VisitRecap is not null || command.YearRecap is not null)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.InvalidSource());
        }

        if (!this.approvalProtector.IsValid(
                command.ApprovalToken,
                ownerUserId,
                command.PublicationType,
                scopeResult.Value,
                command.ApprovedSourceVersion,
                publicationState,
                contentPolicy,
                contentFingerprint))
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.PreviewApprovalInvalid());
        }

        ApplicationResult<long> versionResult = await source.GetCurrentSourceVersionAsync(
            scopeResult.Value,
            cancellationToken);
        if (!versionResult.IsSuccess)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(versionResult.Errors);
        }

        if (versionResult.Value != command.ApprovedSourceVersion)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.ApprovedPreviewExpired());
        }

        return await this.publisher.PublishAsync(
            ownerUserId,
            command.PublicationType,
            scopeResult.Value,
            versionResult.Value,
            publicationState,
            contentPolicy,
            source,
            cancellationToken,
            contentFingerprint,
            normalizedVisitRecap,
            command.SourceId,
            normalizedYearRecap);
    }
}
