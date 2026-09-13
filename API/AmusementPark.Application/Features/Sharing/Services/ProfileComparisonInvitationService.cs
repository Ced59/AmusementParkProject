using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ProfileComparisonInvitationService
{
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    private const int MaximumTokenAttempts = 5;
    private readonly IProfileComparisonInvitationRepository invitationRepository;
    private readonly ProfileComparisonMaterializer comparisonMaterializer;
    private readonly ProfileComparisonPassportResolver passportResolver;
    private readonly IShareTokenFactory tokenFactory;
    private readonly TimeProvider timeProvider;

    public ProfileComparisonInvitationService(
        IProfileComparisonInvitationRepository invitationRepository,
        ProfileComparisonPassportResolver passportResolver,
        ProfileComparisonMaterializer comparisonMaterializer,
        IShareTokenFactory tokenFactory,
        TimeProvider? timeProvider = null)
    {
        this.invitationRepository = invitationRepository
            ?? throw new ArgumentNullException(nameof(invitationRepository));
        this.passportResolver = passportResolver
            ?? throw new ArgumentNullException(nameof(passportResolver));
        this.comparisonMaterializer = comparisonMaterializer
            ?? throw new ArgumentNullException(nameof(comparisonMaterializer));
        this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ProfileComparisonInvitationCreationResult>> CreateAsync(
        string userId,
        IReadOnlyCollection<ProfileComparisonCategory> categories,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = userId?.Trim() ?? string.Empty;
        ProfileComparisonCategory[] normalizedCategories = NormalizeCategories(categories);
        if (normalizedUserId.Length == 0 || normalizedCategories.Length == 0)
        {
            return ApplicationResult<ProfileComparisonInvitationCreationResult>.Failure(
                SharingApplicationErrors.InvalidComparisonCategories());
        }

        ProfileComparisonPassportReference? passport = await this.passportResolver.ResolveCurrentAsync(
            normalizedUserId,
            cancellationToken);
        if (passport is null)
        {
            return ApplicationResult<ProfileComparisonInvitationCreationResult>.Failure(
                SharingApplicationErrors.ComparisonPassportUnavailable());
        }

        if (!ProfileComparisonCategoryPolicy.AllowsAll(
                passport.ContentPolicy,
                normalizedCategories))
        {
            return ApplicationResult<ProfileComparisonInvitationCreationResult>.Failure(
                SharingApplicationErrors.ComparisonCategoriesUnavailable());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        for (int attempt = 0; attempt < MaximumTokenAttempts; attempt++)
        {
            ProfileComparisonInvitation invitation = ProfileComparisonInvitation.Create(
                ProfileComparisonInvitationId.New(),
                this.tokenFactory.Generate(),
                normalizedUserId,
                passport.PublicationId,
                passport.PublicationVersion,
                normalizedCategories,
                nowUtc,
                nowUtc.Add(InvitationLifetime));
            ProfileComparisonInvitationWriteOutcome outcome =
                await this.invitationRepository.CreateAsync(invitation, cancellationToken);
            if (outcome == ProfileComparisonInvitationWriteOutcome.Success)
            {
                return ApplicationResult<ProfileComparisonInvitationCreationResult>.Success(
                    new ProfileComparisonInvitationCreationResult(
                        invitation.Token.Value,
                        invitation.ExpiresAtUtc,
                        invitation.Categories));
            }

            if (outcome != ProfileComparisonInvitationWriteOutcome.TokenCollision)
            {
                break;
            }
        }

        return ApplicationResult<ProfileComparisonInvitationCreationResult>.Failure(
            SharingApplicationErrors.ComparisonInvitationUnavailable());
    }

    public async Task<ApplicationResult<ProfileComparisonInvitationPreviewResult>> PreviewAsync(
        string userId,
        string token,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = userId?.Trim() ?? string.Empty;
        if (normalizedUserId.Length == 0 || !ShareToken.TryParse(token, out ShareToken shareToken))
        {
            return PreviewNotFound();
        }

        ProfileComparisonInvitation? invitation =
            await this.invitationRepository.GetByTokenAsync(shareToken, cancellationToken);
        if (invitation is null)
        {
            return PreviewNotFound();
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (invitation.IsAccepted)
        {
            return ApplicationResult<ProfileComparisonInvitationPreviewResult>.Success(
                new ProfileComparisonInvitationPreviewResult(
                    ProfileComparisonInvitationPreviewStatus.Accepted,
                    null,
                    null,
                    invitation.ExpiresAtUtc,
                    invitation.AcceptedAtUtc,
                    invitation.Categories,
                    false));
        }

        if (invitation.IsExpired(nowUtc))
        {
            return Preview(invitation, ProfileComparisonInvitationPreviewStatus.Expired);
        }

        if (string.Equals(
                invitation.CreatorUserId,
                normalizedUserId,
                StringComparison.Ordinal))
        {
            return Preview(invitation, ProfileComparisonInvitationPreviewStatus.OwnInvitation);
        }

        ProfileComparisonPassportReference? creatorPassport =
            await this.passportResolver.ResolveExactAsync(
                invitation.CreatorPassportPublicationId,
                invitation.CreatorUserId,
                invitation.CreatorPassportPublicationVersion,
                cancellationToken);
        if (creatorPassport is null)
        {
            return Preview(
                invitation,
                ProfileComparisonInvitationPreviewStatus.InviterPassportUnavailable);
        }

        ProfileComparisonPassportReference? inviteePassport =
            await this.passportResolver.ResolveCurrentAsync(
                normalizedUserId,
                cancellationToken);
        if (inviteePassport is null)
        {
            return Preview(
                invitation,
                ProfileComparisonInvitationPreviewStatus.InviteePassportUnavailable,
                creatorPassport.DisplayName);
        }

        if (!ProfileComparisonCategoryPolicy.AllowsAll(
                inviteePassport.ContentPolicy,
                invitation.Categories))
        {
            return ApplicationResult<ProfileComparisonInvitationPreviewResult>.Success(
                new ProfileComparisonInvitationPreviewResult(
                    ProfileComparisonInvitationPreviewStatus.InviteeCategoriesUnavailable,
                    creatorPassport.DisplayName,
                    inviteePassport.DisplayName,
                    invitation.ExpiresAtUtc,
                    null,
                    invitation.Categories,
                    false));
        }

        return ApplicationResult<ProfileComparisonInvitationPreviewResult>.Success(
            new ProfileComparisonInvitationPreviewResult(
                ProfileComparisonInvitationPreviewStatus.Ready,
                creatorPassport.DisplayName,
                inviteePassport.DisplayName,
                invitation.ExpiresAtUtc,
                null,
                invitation.Categories,
                true));
    }

    public async Task<ApplicationResult<ProfileComparisonInvitationAcceptanceResult>> AcceptAsync(
        string userId,
        string token,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = userId?.Trim() ?? string.Empty;
        if (normalizedUserId.Length == 0 || !ShareToken.TryParse(token, out ShareToken shareToken))
        {
            return AcceptanceNotFound();
        }

        ProfileComparisonInvitation? invitation =
            await this.invitationRepository.GetByTokenAsync(shareToken, cancellationToken);
        if (invitation is null)
        {
            return AcceptanceNotFound();
        }

        if (invitation.IsAccepted)
        {
            return string.Equals(
                    invitation.AcceptorUserId,
                    normalizedUserId,
                    StringComparison.Ordinal)
                ? await this.AcceptedAsync(invitation, cancellationToken)
                : AcceptanceNotAcceptable();
        }

        ApplicationResult<ProfileComparisonInvitationPreviewResult> preview =
            await this.PreviewAsync(normalizedUserId, token, cancellationToken);
        if (!preview.IsSuccess
            || preview.Value is null
            || preview.Value.Status != ProfileComparisonInvitationPreviewStatus.Ready)
        {
            return AcceptanceNotAcceptable();
        }

        ProfileComparisonPassportReference? inviteePassport =
            await this.passportResolver.ResolveCurrentAsync(
                normalizedUserId,
                cancellationToken);
        if (inviteePassport is null)
        {
            return AcceptanceNotAcceptable();
        }

        long expectedVersion = invitation.Version;
        try
        {
            invitation.Accept(
                normalizedUserId,
                inviteePassport.PublicationId,
                inviteePassport.PublicationVersion,
                ProfileComparisonId.New(),
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (ProfileComparisonInvitationValidationException)
        {
            return AcceptanceNotAcceptable();
        }
        ProfileComparisonInvitationWriteOutcome outcome =
            await this.invitationRepository.ReplaceAsync(
                invitation,
                expectedVersion,
                cancellationToken);
        if (outcome == ProfileComparisonInvitationWriteOutcome.Success)
        {
            bool passportsRemainCurrent = await this.PassportsRemainCurrentAsync(
                invitation,
                cancellationToken);
            if (!passportsRemainCurrent)
            {
                bool compensated = await this.invitationRepository.DeleteAcceptedAsync(
                    invitation.Id,
                    invitation.Version,
                    cancellationToken);
                return compensated
                    ? AcceptanceNotAcceptable()
                    : ApplicationResult<ProfileComparisonInvitationAcceptanceResult>.Failure(
                        SharingApplicationErrors.ComparisonInvitationChangedConcurrently());
            }

            return await this.AcceptedAsync(invitation, cancellationToken);
        }

        ProfileComparisonInvitation? current =
            await this.invitationRepository.GetByTokenAsync(shareToken, cancellationToken);
        if (current?.IsAccepted == true
            && string.Equals(current.AcceptorUserId, normalizedUserId, StringComparison.Ordinal))
        {
            return await this.AcceptedAsync(current, cancellationToken);
        }

        return ApplicationResult<ProfileComparisonInvitationAcceptanceResult>.Failure(
            SharingApplicationErrors.ComparisonInvitationChangedConcurrently());
    }

    private async Task<bool> PassportsRemainCurrentAsync(
        ProfileComparisonInvitation invitation,
        CancellationToken cancellationToken)
    {
        if (!invitation.AcceptorPassportPublicationId.HasValue
            || !invitation.AcceptorPassportPublicationVersion.HasValue
            || string.IsNullOrWhiteSpace(invitation.AcceptorUserId))
        {
            return false;
        }

        ProfileComparisonPassportReference? creatorPassport =
            await this.passportResolver.ResolveExactAsync(
                invitation.CreatorPassportPublicationId,
                invitation.CreatorUserId,
                invitation.CreatorPassportPublicationVersion,
                cancellationToken);
        if (creatorPassport is null
            || !ProfileComparisonCategoryPolicy.AllowsAll(
                creatorPassport.ContentPolicy,
                invitation.Categories))
        {
            return false;
        }

        ProfileComparisonPassportReference? acceptorPassport =
            await this.passportResolver.ResolveExactAsync(
                invitation.AcceptorPassportPublicationId.Value,
                invitation.AcceptorUserId,
                invitation.AcceptorPassportPublicationVersion.Value,
                cancellationToken);
        return acceptorPassport is not null
            && ProfileComparisonCategoryPolicy.AllowsAll(
                acceptorPassport.ContentPolicy,
                invitation.Categories);
    }

    private static ProfileComparisonCategory[] NormalizeCategories(
        IReadOnlyCollection<ProfileComparisonCategory>? categories)
    {
        if (categories is null || categories.Any(static category => !Enum.IsDefined(category)))
        {
            return Array.Empty<ProfileComparisonCategory>();
        }

        ProfileComparisonCategory[] normalized = categories
            .Distinct()
            .OrderBy(static category => category)
            .ToArray();
        return normalized.Length <= ProfileComparisonInvitation.MaximumCategories
            ? normalized
            : Array.Empty<ProfileComparisonCategory>();
    }

    private static ApplicationResult<ProfileComparisonInvitationPreviewResult> Preview(
        ProfileComparisonInvitation invitation,
        ProfileComparisonInvitationPreviewStatus status,
        string? creatorDisplayName = null)
    {
        return ApplicationResult<ProfileComparisonInvitationPreviewResult>.Success(
            new ProfileComparisonInvitationPreviewResult(
                status,
                creatorDisplayName,
                null,
                invitation.ExpiresAtUtc,
                invitation.AcceptedAtUtc,
                invitation.Categories,
                false));
    }

    private static ApplicationResult<ProfileComparisonInvitationPreviewResult> PreviewNotFound()
    {
        return ApplicationResult<ProfileComparisonInvitationPreviewResult>.Failure(
            SharingApplicationErrors.ComparisonInvitationNotFound());
    }

    private async Task<ApplicationResult<ProfileComparisonInvitationAcceptanceResult>> AcceptedAsync(
        ProfileComparisonInvitation invitation,
        CancellationToken cancellationToken)
    {
        ApplicationResult<ProfileComparison> materialization =
            await this.comparisonMaterializer.MaterializeAsync(invitation, cancellationToken);
        if (!materialization.IsSuccess || materialization.Value is null)
        {
            return ApplicationResult<ProfileComparisonInvitationAcceptanceResult>.Failure(
                materialization.Errors);
        }

        return ApplicationResult<ProfileComparisonInvitationAcceptanceResult>.Success(
            new ProfileComparisonInvitationAcceptanceResult(
                materialization.Value.ShareToken.Value,
                invitation.AcceptedAtUtc!.Value,
                invitation.Categories));
    }

    private static ApplicationResult<ProfileComparisonInvitationAcceptanceResult>
        AcceptanceNotFound()
    {
        return ApplicationResult<ProfileComparisonInvitationAcceptanceResult>.Failure(
            SharingApplicationErrors.ComparisonInvitationNotFound());
    }

    private static ApplicationResult<ProfileComparisonInvitationAcceptanceResult>
        AcceptanceNotAcceptable()
    {
        return ApplicationResult<ProfileComparisonInvitationAcceptanceResult>.Failure(
            SharingApplicationErrors.ComparisonInvitationNotAcceptable());
    }
}
