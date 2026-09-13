using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Preuve des deux consentements nécessaires avant toute comparaison de passeports publics.
/// </summary>
public sealed class ProfileComparisonInvitation
{
    public const int MaximumCategories = 4;

    public static readonly TimeSpan MinimumLifetime = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromDays(30);

    private readonly IReadOnlyList<ProfileComparisonCategory> categories;

    private ProfileComparisonInvitation(
        ProfileComparisonInvitationId id,
        ShareToken token,
        string creatorUserId,
        SharePublicationId creatorPassportPublicationId,
        long creatorPassportPublicationVersion,
        IEnumerable<ProfileComparisonCategory> categories,
        ProfileComparisonInvitationStatus status,
        string? acceptorUserId,
        SharePublicationId? acceptorPassportPublicationId,
        long? acceptorPassportPublicationVersion,
        ProfileComparisonId? comparisonId,
        DateTime expiresAtUtc,
        DateTime? acceptedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        _ = id.Value;
        _ = token.Value;
        string normalizedCreatorUserId = IdentifierRules.NormalizeRequired(
            creatorUserId,
            nameof(creatorUserId));
        _ = creatorPassportPublicationId.Value;
        ValidatePositiveVersion(
            creatorPassportPublicationVersion,
            nameof(creatorPassportPublicationVersion));
        ProfileComparisonCategory[] normalizedCategories = NormalizeCategories(categories);
        ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        ValidateUtc(updatedAtUtc, nameof(updatedAtUtc));
        ValidateUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (acceptedAtUtc.HasValue)
        {
            ValidateUtc(acceptedAtUtc.Value, nameof(acceptedAtUtc));
        }

        ValidateLifetime(createdAtUtc, expiresAtUtc);
        if (updatedAtUtc < createdAtUtc || version < 0)
        {
            throw InvalidState();
        }

        ValidateAcceptedState(
            status,
            normalizedCreatorUserId,
            acceptorUserId,
            acceptorPassportPublicationId,
            acceptorPassportPublicationVersion,
            comparisonId,
            acceptedAtUtc,
            createdAtUtc,
            updatedAtUtc,
            expiresAtUtc);

        this.Id = id;
        this.Token = token;
        this.CreatorUserId = normalizedCreatorUserId;
        this.CreatorPassportPublicationId = creatorPassportPublicationId;
        this.CreatorPassportPublicationVersion = creatorPassportPublicationVersion;
        this.categories = Array.AsReadOnly(normalizedCategories);
        this.Status = status;
        this.AcceptorUserId = acceptorUserId?.Trim();
        this.AcceptorPassportPublicationId = acceptorPassportPublicationId;
        this.AcceptorPassportPublicationVersion = acceptorPassportPublicationVersion;
        this.ComparisonId = comparisonId;
        this.ExpiresAtUtc = expiresAtUtc;
        this.AcceptedAtUtc = acceptedAtUtc;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
    }

    public ProfileComparisonInvitationId Id { get; }

    public ShareToken Token { get; }

    public string CreatorUserId { get; }

    public SharePublicationId CreatorPassportPublicationId { get; }

    public long CreatorPassportPublicationVersion { get; }

    public IReadOnlyList<ProfileComparisonCategory> Categories => this.categories;

    public ProfileComparisonInvitationStatus Status { get; private set; }

    public string? AcceptorUserId { get; private set; }

    public SharePublicationId? AcceptorPassportPublicationId { get; private set; }

    public long? AcceptorPassportPublicationVersion { get; private set; }

    public ProfileComparisonId? ComparisonId { get; private set; }

    public DateTime ExpiresAtUtc { get; }

    public DateTime? AcceptedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public bool IsAccepted => this.Status == ProfileComparisonInvitationStatus.Accepted;

    public static ProfileComparisonInvitation Create(
        ProfileComparisonInvitationId id,
        ShareToken token,
        string creatorUserId,
        SharePublicationId creatorPassportPublicationId,
        long creatorPassportPublicationVersion,
        IEnumerable<ProfileComparisonCategory> categories,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        return new ProfileComparisonInvitation(
            id,
            token,
            creatorUserId,
            creatorPassportPublicationId,
            creatorPassportPublicationVersion,
            categories,
            ProfileComparisonInvitationStatus.Pending,
            null,
            null,
            null,
            null,
            expiresAtUtc,
            null,
            createdAtUtc,
            createdAtUtc,
            0);
    }

    public static ProfileComparisonInvitation Restore(
        ProfileComparisonInvitationId id,
        ShareToken token,
        string creatorUserId,
        SharePublicationId creatorPassportPublicationId,
        long creatorPassportPublicationVersion,
        IEnumerable<ProfileComparisonCategory> categories,
        ProfileComparisonInvitationStatus status,
        string? acceptorUserId,
        SharePublicationId? acceptorPassportPublicationId,
        long? acceptorPassportPublicationVersion,
        ProfileComparisonId? comparisonId,
        DateTime expiresAtUtc,
        DateTime? acceptedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        return new ProfileComparisonInvitation(
            id,
            token,
            creatorUserId,
            creatorPassportPublicationId,
            creatorPassportPublicationVersion,
            categories,
            status,
            acceptorUserId,
            acceptorPassportPublicationId,
            acceptorPassportPublicationVersion,
            comparisonId,
            expiresAtUtc,
            acceptedAtUtc,
            createdAtUtc,
            updatedAtUtc,
            version);
    }

    public bool IsExpired(DateTime nowUtc)
    {
        ValidateUtc(nowUtc, nameof(nowUtc));
        return !this.IsAccepted && nowUtc >= this.ExpiresAtUtc;
    }

    public void Accept(
        string acceptorUserId,
        SharePublicationId acceptorPassportPublicationId,
        long acceptorPassportPublicationVersion,
        ProfileComparisonId comparisonId,
        DateTime acceptedAtUtc)
    {
        string normalizedAcceptorUserId = IdentifierRules.NormalizeRequired(
            acceptorUserId,
            nameof(acceptorUserId));
        ValidateUtc(acceptedAtUtc, nameof(acceptedAtUtc));
        ValidatePositiveVersion(
            acceptorPassportPublicationVersion,
            nameof(acceptorPassportPublicationVersion));
        _ = acceptorPassportPublicationId.Value;
        _ = comparisonId.Value;
        if (this.IsAccepted)
        {
            throw new ProfileComparisonInvitationValidationException(
                ProfileComparisonInvitationErrorCodes.AlreadyAccepted,
                "The comparison invitation has already been accepted.");
        }

        if (acceptedAtUtc >= this.ExpiresAtUtc)
        {
            throw new ProfileComparisonInvitationValidationException(
                ProfileComparisonInvitationErrorCodes.Expired,
                "The comparison invitation has expired.");
        }

        if (string.Equals(
                this.CreatorUserId,
                normalizedAcceptorUserId,
                StringComparison.Ordinal))
        {
            throw new ProfileComparisonInvitationValidationException(
                ProfileComparisonInvitationErrorCodes.SelfAcceptance,
                "A comparison invitation cannot be accepted by its creator.");
        }

        if (this.Version == long.MaxValue)
        {
            throw InvalidState();
        }

        this.AcceptorUserId = normalizedAcceptorUserId;
        this.AcceptorPassportPublicationId = acceptorPassportPublicationId;
        this.AcceptorPassportPublicationVersion = acceptorPassportPublicationVersion;
        this.ComparisonId = comparisonId;
        this.AcceptedAtUtc = acceptedAtUtc;
        this.Status = ProfileComparisonInvitationStatus.Accepted;
        this.UpdatedAtUtc = acceptedAtUtc;
        this.Version++;
    }

    private static ProfileComparisonCategory[] NormalizeCategories(
        IEnumerable<ProfileComparisonCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ProfileComparisonCategory[] normalized = categories
            .Distinct()
            .OrderBy(static category => category)
            .ToArray();
        if (normalized.Length == 0 || normalized.Length > MaximumCategories)
        {
            throw new ProfileComparisonInvitationValidationException(
                ProfileComparisonInvitationErrorCodes.InvalidCategoryCount,
                "A comparison invitation must select between one and four categories.");
        }

        if (normalized.Any(static category => !Enum.IsDefined(category)))
        {
            throw new ProfileComparisonInvitationValidationException(
                ProfileComparisonInvitationErrorCodes.InvalidCategory,
                "A comparison invitation category is invalid.");
        }

        return normalized;
    }

    private static void ValidateLifetime(DateTime createdAtUtc, DateTime expiresAtUtc)
    {
        TimeSpan lifetime = expiresAtUtc - createdAtUtc;
        if (lifetime < MinimumLifetime || lifetime > MaximumLifetime)
        {
            throw new ProfileComparisonInvitationValidationException(
                ProfileComparisonInvitationErrorCodes.InvalidLifetime,
                "The comparison invitation lifetime is invalid.");
        }
    }

    private static void ValidateAcceptedState(
        ProfileComparisonInvitationStatus status,
        string creatorUserId,
        string? acceptorUserId,
        SharePublicationId? acceptorPassportPublicationId,
        long? acceptorPassportPublicationVersion,
        ProfileComparisonId? comparisonId,
        DateTime? acceptedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime expiresAtUtc)
    {
        if (!Enum.IsDefined(status))
        {
            throw InvalidState();
        }

        bool hasCompleteAcceptance = !string.IsNullOrWhiteSpace(acceptorUserId)
            && acceptorPassportPublicationId.HasValue
            && acceptorPassportPublicationVersion > 0
            && comparisonId.HasValue
            && acceptedAtUtc.HasValue;
        if ((status == ProfileComparisonInvitationStatus.Accepted) != hasCompleteAcceptance)
        {
            throw InvalidState();
        }

        if (!hasCompleteAcceptance)
        {
            return;
        }

        string normalizedAcceptorUserId = IdentifierRules.NormalizeRequired(
            acceptorUserId,
            nameof(acceptorUserId));
        if (string.Equals(creatorUserId, normalizedAcceptorUserId, StringComparison.Ordinal)
            || acceptedAtUtc!.Value < createdAtUtc
            || acceptedAtUtc.Value >= expiresAtUtc
            || updatedAtUtc < acceptedAtUtc.Value)
        {
            throw InvalidState();
        }

        _ = acceptorPassportPublicationId!.Value.Value;
        _ = comparisonId!.Value.Value;
    }

    private static void ValidatePositiveVersion(long version, string parameterName)
    {
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must use UTC.", parameterName);
        }
    }

    private static ProfileComparisonInvitationValidationException InvalidState()
    {
        return new ProfileComparisonInvitationValidationException(
            ProfileComparisonInvitationErrorCodes.InvalidState,
            "The comparison invitation state is invalid.");
    }
}
