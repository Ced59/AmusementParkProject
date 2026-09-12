using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class SharePublicationAccessResolver : ISharePublicationAccessResolver
{
    private readonly ISharePublicationRepository sharePublicationRepository;
    private readonly IUserRepository userRepository;
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSourceDescriptor> sources;

    public SharePublicationAccessResolver(
        ISharePublicationRepository sharePublicationRepository,
        IUserRepository userRepository,
        IEnumerable<ISharePublicationSourceDescriptor> sources)
    {
        this.sharePublicationRepository = sharePublicationRepository
            ?? throw new ArgumentNullException(nameof(sharePublicationRepository));
        this.userRepository = userRepository
            ?? throw new ArgumentNullException(nameof(userRepository));
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources.ToDictionary(static source => source.PublicationType);
    }

    public async Task<ApplicationResult<ResolvedSharePublicationResult>> ResolveAsync(
        string shareId,
        SharePublicationType expectedPublicationType,
        CancellationToken cancellationToken)
    {
        string normalizedShareId = shareId?.Trim() ?? string.Empty;
        if (!ShareToken.TryParse(normalizedShareId, out ShareToken shareToken))
        {
            return NotFound(expectedPublicationType);
        }

        SharePublication? publication = await this.sharePublicationRepository.GetResolvableByTokenAsync(
            shareToken,
            cancellationToken);
        if (publication is null
            || publication.Type != expectedPublicationType
            || publication.PublishedAtUtc is null)
        {
            return NotFound(expectedPublicationType);
        }

        if (!this.sources.TryGetValue(publication.Type, out ISharePublicationSourceDescriptor? source)
            || !source.ValidatePolicyForPublication(publication.ContentPolicy).IsSuccess)
        {
            return NotFound(expectedPublicationType);
        }

        ApplicationResult<long> sourceVersionResult = await source.GetCurrentSourceVersionAsync(
            publication.SourceScopeKey,
            publication.ContentPolicy,
            cancellationToken);
        if (!sourceVersionResult.IsSuccess
            || sourceVersionResult.Value != publication.SourceVersion)
        {
            return NotFound(expectedPublicationType);
        }

        User? user = await this.userRepository.GetByIdAsync(
            publication.OwnerUserId,
            cancellationToken);
        if (user is null || !user.IsActivated || user.IsBlocked)
        {
            return NotFound(expectedPublicationType);
        }

        string? displayName = publication.ContentPolicy.Includes(
            ShareContentField.PublicDisplayName)
            ? user.ResolvePublicDisplayName()?.Trim()
            : null;
        displayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;

        ResolvedSharePublicationResult resolvedPublication = new ResolvedSharePublicationResult(
            user.Id,
            displayName,
            publication.Type,
            publication.ContentPolicy,
            publication.PublishedAtUtc.Value,
            publication.SourceScopeKey,
            publication.SourceVersion,
            publication.PublicationVersion,
            publication.Id.Value,
            publication.ContentFingerprint);
        return ApplicationResult<ResolvedSharePublicationResult>.Success(resolvedPublication);
    }

    public async Task<ApplicationResult<bool>> RevalidateAsync(
        string shareId,
        ResolvedSharePublicationResult resolvedPublication,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolvedPublication);
        string normalizedShareId = shareId?.Trim() ?? string.Empty;
        if (!ShareToken.TryParse(normalizedShareId, out ShareToken shareToken))
        {
            return RevalidationFailed(resolvedPublication.PublicationType);
        }

        SharePublication? currentPublication = await this.sharePublicationRepository.GetResolvableByTokenAsync(
            shareToken,
            cancellationToken);
        if (currentPublication is null
            || currentPublication.Type != resolvedPublication.PublicationType
            || !string.Equals(
                currentPublication.OwnerUserId,
                resolvedPublication.OwnerUserId,
                StringComparison.Ordinal)
            || !string.Equals(
                currentPublication.SourceScopeKey,
                resolvedPublication.SourceScopeKey,
                StringComparison.Ordinal)
            || currentPublication.SourceVersion != resolvedPublication.SourceVersion
            || currentPublication.PublicationVersion != resolvedPublication.PublicationVersion
            || !string.Equals(
                currentPublication.ContentFingerprint,
                resolvedPublication.ContentFingerprint,
                StringComparison.Ordinal)
            || currentPublication.PublishedAtUtc != resolvedPublication.PublishedAtUtc
            || !currentPublication.ContentPolicy.HasSameSelectionAs(
                resolvedPublication.ContentPolicy)
            || !this.sources.TryGetValue(
                currentPublication.Type,
                out ISharePublicationSourceDescriptor? source))
        {
            return RevalidationFailed(resolvedPublication.PublicationType);
        }

        ApplicationResult<long> currentVersion = await source.GetCurrentSourceVersionAsync(
            currentPublication.SourceScopeKey,
            currentPublication.ContentPolicy,
            cancellationToken);
        return currentVersion.IsSuccess
            && currentVersion.Value == currentPublication.SourceVersion
            ? ApplicationResult<bool>.Success(true)
            : RevalidationFailed(resolvedPublication.PublicationType);
    }

    private static ApplicationResult<ResolvedSharePublicationResult> NotFound(
        SharePublicationType publicationType)
    {
        return ApplicationResult<ResolvedSharePublicationResult>.Failure(
            NotFoundError(publicationType));
    }

    private static ApplicationResult<bool> RevalidationFailed(
        SharePublicationType publicationType)
    {
        return ApplicationResult<bool>.Failure(
            NotFoundError(publicationType));
    }

    private static ApplicationError NotFoundError(
        SharePublicationType publicationType)
    {
        return publicationType == SharePublicationType.PersonalRanking
            ? RatingApplicationErrors.SharedRankingNotFound()
            : SharingApplicationErrors.SharedPublicationNotFound();
    }
}
