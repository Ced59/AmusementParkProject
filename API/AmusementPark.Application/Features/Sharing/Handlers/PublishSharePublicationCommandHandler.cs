using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
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
    private readonly ISharePublicationPreviewApprovalProtector approvalProtector;

    public PublishSharePublicationCommandHandler(
        IEnumerable<ISharePublicationSourceDescriptor> sources,
        SharePublicationPublisher publisher,
        ISharePublicationPreviewApprovalProtector approvalProtector)
    {
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources.ToDictionary(static source => source.PublicationType);
        this.publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
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

        if (!this.approvalProtector.IsValid(
                command.ApprovalToken,
                ownerUserId,
                command.PublicationType,
                scopeResult.Value,
                command.ApprovedSourceVersion,
                contentPolicy))
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
            contentPolicy,
            cancellationToken);
    }
}
