using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ProfileComparisonMaterializer
{
    private const int MaximumTokenAttempts = 5;
    private readonly IProfileComparisonRepository comparisonRepository;
    private readonly ProfileComparisonPassportResolver passportResolver;
    private readonly IShareTokenFactory tokenFactory;
    private readonly TimeProvider timeProvider;

    public ProfileComparisonMaterializer(
        IProfileComparisonRepository comparisonRepository,
        ProfileComparisonPassportResolver passportResolver,
        IShareTokenFactory tokenFactory,
        TimeProvider? timeProvider = null)
    {
        this.comparisonRepository = comparisonRepository
            ?? throw new ArgumentNullException(nameof(comparisonRepository));
        this.passportResolver = passportResolver
            ?? throw new ArgumentNullException(nameof(passportResolver));
        this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ProfileComparison>> MaterializeAsync(
        ProfileComparisonInvitation invitation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        if (!invitation.IsAccepted
            || !invitation.ComparisonId.HasValue
            || string.IsNullOrWhiteSpace(invitation.AcceptorUserId)
            || !invitation.AcceptorPassportPublicationId.HasValue
            || !invitation.AcceptorPassportPublicationVersion.HasValue)
        {
            return Unavailable();
        }

        ProfileComparison? existing = await this.comparisonRepository.GetByIdAsync(
            invitation.ComparisonId.Value,
            cancellationToken);
        if (existing is not null)
        {
            return await this.ValidateExistingAsync(
                existing,
                invitation,
                cancellationToken);
        }

        ProfileComparisonPassportReference? creatorPassport =
            await this.passportResolver.ResolveExactAsync(
                invitation.CreatorPassportPublicationId,
                invitation.CreatorUserId,
                invitation.CreatorPassportPublicationVersion,
                cancellationToken);
        ProfileComparisonPassportReference? acceptorPassport =
            await this.passportResolver.ResolveExactAsync(
                invitation.AcceptorPassportPublicationId.Value,
                invitation.AcceptorUserId,
                invitation.AcceptorPassportPublicationVersion.Value,
                cancellationToken);
        if (!IsUsable(creatorPassport, invitation.Categories)
            || !IsUsable(acceptorPassport, invitation.Categories))
        {
            return ApplicationResult<ProfileComparison>.Failure(
                SharingApplicationErrors.ComparisonPassportsChanged());
        }

        ProfileComparisonCalculation calculation = ProfileComparisonCalculator.Calculate(
            creatorPassport!.ToComparisonData(),
            acceptorPassport!.ToComparisonData(),
            invitation.Categories);
        for (int attempt = 0; attempt < MaximumTokenAttempts; attempt++)
        {
            ProfileComparison comparison = ProfileComparison.Create(
                invitation.ComparisonId.Value,
                invitation.Id,
                this.tokenFactory.Generate(),
                invitation.CreatorUserId,
                invitation.AcceptorUserId,
                invitation.CreatorPassportPublicationId,
                invitation.CreatorPassportPublicationVersion,
                invitation.AcceptorPassportPublicationId.Value,
                invitation.AcceptorPassportPublicationVersion.Value,
                calculation,
                this.timeProvider.GetUtcNow().UtcDateTime);
            ProfileComparisonWriteOutcome outcome = await this.comparisonRepository.CreateAsync(
                comparison,
                cancellationToken);
            if (outcome == ProfileComparisonWriteOutcome.Success)
            {
                return ApplicationResult<ProfileComparison>.Success(comparison);
            }

            ProfileComparison? concurrentlyCreated = await this.comparisonRepository.GetByIdAsync(
                invitation.ComparisonId.Value,
                cancellationToken);
            if (concurrentlyCreated is not null)
            {
                return await this.ValidateExistingAsync(
                    concurrentlyCreated,
                    invitation,
                    cancellationToken);
            }

            if (outcome != ProfileComparisonWriteOutcome.TokenCollision)
            {
                break;
            }
        }

        return Unavailable();
    }

    private async Task<ApplicationResult<ProfileComparison>> ValidateExistingAsync(
        ProfileComparison comparison,
        ProfileComparisonInvitation invitation,
        CancellationToken cancellationToken)
    {
        if (comparison.InvitationId != invitation.Id || !comparison.IsActive)
        {
            return Unavailable();
        }

        ProfileComparisonPassportReference? creatorPassport =
            await this.passportResolver.ResolveExactAsync(
                comparison.CreatorPassportPublicationId,
                comparison.CreatorUserId,
                comparison.CreatorPassportPublicationVersion,
                cancellationToken);
        ProfileComparisonPassportReference? acceptorPassport =
            await this.passportResolver.ResolveExactAsync(
                comparison.AcceptorPassportPublicationId,
                comparison.AcceptorUserId,
                comparison.AcceptorPassportPublicationVersion,
                cancellationToken);
        if (!IsUsable(creatorPassport, comparison.Calculation.Categories)
            || !IsUsable(acceptorPassport, comparison.Calculation.Categories))
        {
            return ApplicationResult<ProfileComparison>.Failure(
                SharingApplicationErrors.ComparisonPassportsChanged());
        }

        return ApplicationResult<ProfileComparison>.Success(comparison);
    }

    private static bool IsUsable(
        ProfileComparisonPassportReference? passport,
        IReadOnlyCollection<ProfileComparisonCategory> categories)
    {
        return passport is not null
            && passport.Snapshot.Content.AllowsComparisons
            && ProfileComparisonCategoryPolicy.AllowsAll(passport.ContentPolicy, categories);
    }

    private static ApplicationResult<ProfileComparison> Unavailable()
    {
        return ApplicationResult<ProfileComparison>.Failure(
            SharingApplicationErrors.ComparisonUnavailable());
    }
}
