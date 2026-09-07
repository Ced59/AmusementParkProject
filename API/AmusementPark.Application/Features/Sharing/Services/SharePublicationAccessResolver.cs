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
            return NotFound();
        }

        SharePublication? publication = await this.sharePublicationRepository.GetResolvableByTokenAsync(
            shareToken,
            cancellationToken);
        if (publication is null
            || publication.Type != expectedPublicationType
            || publication.PublishedAtUtc is null)
        {
            return NotFound();
        }

        if (!this.sources.TryGetValue(publication.Type, out ISharePublicationSourceDescriptor? source)
            || !source.ValidatePolicyForPublication(publication.ContentPolicy).IsSuccess)
        {
            return NotFound();
        }

        ApplicationResult<long> sourceVersionResult = await source.GetCurrentSourceVersionAsync(
            publication.SourceScopeKey,
            cancellationToken);
        if (!sourceVersionResult.IsSuccess
            || sourceVersionResult.Value != publication.SourceVersion)
        {
            return NotFound();
        }

        User? user = await this.userRepository.GetByIdAsync(
            publication.OwnerUserId,
            cancellationToken);
        if (user is null || !user.IsActivated || user.IsBlocked)
        {
            return NotFound();
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
            publication.PublicationVersion);
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
            return RevalidationFailed();
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
            || currentPublication.PublishedAtUtc != resolvedPublication.PublishedAtUtc
            || !currentPublication.ContentPolicy.HasSameSelectionAs(
                resolvedPublication.ContentPolicy)
            || !this.sources.TryGetValue(
                currentPublication.Type,
                out ISharePublicationSourceDescriptor? source))
        {
            return RevalidationFailed();
        }

        ApplicationResult<long> currentVersion = await source.GetCurrentSourceVersionAsync(
            currentPublication.SourceScopeKey,
            cancellationToken);
        return currentVersion.IsSuccess
            && currentVersion.Value == currentPublication.SourceVersion
            ? ApplicationResult<bool>.Success(true)
            : RevalidationFailed();
    }

    private static ApplicationResult<ResolvedSharePublicationResult> NotFound()
    {
        return ApplicationResult<ResolvedSharePublicationResult>.Failure(
            RatingApplicationErrors.SharedRankingNotFound());
    }

    private static ApplicationResult<bool> RevalidationFailed()
    {
        return ApplicationResult<bool>.Failure(
            RatingApplicationErrors.SharedRankingNotFound());
    }
}
