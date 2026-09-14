using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Profil privé réutilisable contenant uniquement les faits déjà compris par Park Fit.
/// </summary>
public sealed class ParkFitGroupProfile
{
    public const int MaximumAliasLength = 60;

    private ParkFitGroupProfile(
        ParkFitGroupProfileId id,
        string ownerUserId,
        string alias,
        int? heightCentimeters,
        int? ageYears,
        bool canBeAccompanied,
        int? companionAgeYears,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        _ = id.Value;
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        string normalizedAlias = NormalizeAlias(alias);
        ValidateFacts(
            heightCentimeters,
            ageYears,
            canBeAccompanied,
            companionAgeYears);
        ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        ValidateUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < createdAtUtc || version < 1)
        {
            throw InvalidState();
        }

        this.Id = id;
        this.OwnerUserId = normalizedOwnerUserId;
        this.Alias = normalizedAlias;
        this.NormalizedAlias = normalizedAlias.ToUpperInvariant();
        this.HeightCentimeters = heightCentimeters;
        this.AgeYears = ageYears;
        this.CanBeAccompanied = canBeAccompanied;
        this.CompanionAgeYears = canBeAccompanied ? companionAgeYears : null;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
    }

    public ParkFitGroupProfileId Id { get; }

    public string OwnerUserId { get; }

    public string Alias { get; private set; }

    public string NormalizedAlias { get; private set; }

    public int? HeightCentimeters { get; private set; }

    public int? AgeYears { get; private set; }

    public bool CanBeAccompanied { get; private set; }

    public int? CompanionAgeYears { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static ParkFitGroupProfile Create(
        ParkFitGroupProfileId id,
        string ownerUserId,
        string alias,
        int? heightCentimeters,
        int? ageYears,
        bool canBeAccompanied,
        int? companionAgeYears,
        DateTime createdAtUtc)
    {
        return new ParkFitGroupProfile(
            id,
            ownerUserId,
            alias,
            heightCentimeters,
            ageYears,
            canBeAccompanied,
            companionAgeYears,
            createdAtUtc,
            createdAtUtc,
            1);
    }

    public static ParkFitGroupProfile Restore(
        ParkFitGroupProfileId id,
        string ownerUserId,
        string alias,
        int? heightCentimeters,
        int? ageYears,
        bool canBeAccompanied,
        int? companionAgeYears,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        return new ParkFitGroupProfile(
            id,
            ownerUserId,
            alias,
            heightCentimeters,
            ageYears,
            canBeAccompanied,
            companionAgeYears,
            createdAtUtc,
            updatedAtUtc,
            version);
    }

    public void Update(
        string alias,
        int? heightCentimeters,
        int? ageYears,
        bool canBeAccompanied,
        int? companionAgeYears,
        DateTime updatedAtUtc)
    {
        string normalizedAlias = NormalizeAlias(alias);
        ValidateFacts(
            heightCentimeters,
            ageYears,
            canBeAccompanied,
            companionAgeYears);
        ValidateUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < this.UpdatedAtUtc || this.Version == long.MaxValue)
        {
            throw InvalidState();
        }

        this.Alias = normalizedAlias;
        this.NormalizedAlias = normalizedAlias.ToUpperInvariant();
        this.HeightCentimeters = heightCentimeters;
        this.AgeYears = ageYears;
        this.CanBeAccompanied = canBeAccompanied;
        this.CompanionAgeYears = canBeAccompanied ? companionAgeYears : null;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version++;
    }

    private static string NormalizeAlias(string? alias)
    {
        string normalized = string.Join(
            ' ',
            (alias ?? string.Empty).Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (normalized.Length is < 1 or > MaximumAliasLength)
        {
            throw new ParkFitGroupProfileValidationException(
                ParkFitGroupProfileErrorCodes.InvalidAlias,
                $"Alias must contain between 1 and {MaximumAliasLength} characters.");
        }

        return normalized;
    }

    private static void ValidateFacts(
        int? heightCentimeters,
        int? ageYears,
        bool canBeAccompanied,
        int? companionAgeYears)
    {
        if (heightCentimeters is < ParkFitMemberProfile.MinimumSupportedHeightCentimeters
            or > ParkFitMemberProfile.MaximumSupportedHeightCentimeters
            || ageYears is < 0 or > ParkFitAgeRange.MaximumSupportedAgeYears
            || companionAgeYears is < 0 or > ParkFitAgeRange.MaximumSupportedAgeYears
            || companionAgeYears.HasValue && !canBeAccompanied)
        {
            throw new ParkFitGroupProfileValidationException(
                ParkFitGroupProfileErrorCodes.InvalidFacts,
                "The Park Fit member facts are invalid.");
        }
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must use UTC.", parameterName);
        }
    }

    private static ParkFitGroupProfileValidationException InvalidState()
    {
        return new ParkFitGroupProfileValidationException(
            ParkFitGroupProfileErrorCodes.InvalidState,
            "The Park Fit group profile state is invalid.");
    }
}
