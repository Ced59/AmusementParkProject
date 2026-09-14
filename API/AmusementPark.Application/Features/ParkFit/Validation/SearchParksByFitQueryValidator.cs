using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Validation;

/// <summary>
/// Borne strictement le calcul anonyme avant tout accès aux données.
/// </summary>
public sealed class SearchParksByFitQueryValidator
    : IApplicationValidator<SearchParksByFitQuery>
{
    public IReadOnlyCollection<ApplicationError> Validate(SearchParksByFitQuery request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<ApplicationError> errors = new List<ApplicationError>();
        if (request.EvaluationDate == default)
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(request.EvaluationDate),
                "An evaluation date is required."));
        }

        ValidateMembers(request.Members, errors);
        ValidatePreferences(request.PreferredAttractionTypes, errors);
        ValidateOrigin(request, errors);

        if (!string.IsNullOrWhiteSpace(request.CountryCode)
            && !IsCountryCode(request.CountryCode))
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(request.CountryCode),
                "Country code must contain exactly two letters."));
        }

        if (!Enum.IsDefined(request.UnknownDataPolicy))
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(request.UnknownDataPolicy),
                "Unknown data policy is invalid."));
        }

        if (request.MaximumResults is < 1 or > ParkFitSearchLimits.MaximumResultCount)
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(request.MaximumResults),
                $"Maximum results must be between 1 and {ParkFitSearchLimits.MaximumResultCount}."));
        }

        return errors;
    }

    private static void ValidateMembers(
        IReadOnlyCollection<ParkFitSearchMemberCriteria>? members,
        ICollection<ApplicationError> errors)
    {
        if (members is null || members.Count is < 1 or > ParkFitSearchLimits.MaximumMemberCount)
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(SearchParksByFitQuery.Members),
                $"A search requires between 1 and {ParkFitSearchLimits.MaximumMemberCount} members."));
            return;
        }

        if (members.Any(static member => member is null))
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(SearchParksByFitQuery.Members),
                "Members cannot contain null entries."));
            return;
        }

        HashSet<string> memberKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (ParkFitSearchMemberCriteria member in members)
        {
            ValidateMember(member, memberKeys, errors);
        }
    }

    private static void ValidateMember(
        ParkFitSearchMemberCriteria member,
        ISet<string> memberKeys,
        ICollection<ApplicationError> errors)
    {
        string normalizedKey = member.MemberKey?.Trim() ?? string.Empty;
        if (normalizedKey.Length is < 1 or > ParkFitSearchLimits.MaximumMemberKeyLength
            || normalizedKey.Any(static character => !char.IsAsciiLetterOrDigit(character)
                && character is not '-' and not '_'))
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(ParkFitSearchMemberCriteria.MemberKey),
                "Member keys must be opaque identifiers containing only letters, digits, '-' or '_'."));
        }
        else if (!memberKeys.Add(normalizedKey))
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(ParkFitSearchMemberCriteria.MemberKey),
                "Member keys must be unique within one search."));
        }

        if (member.HeightCentimeters is < ParkFitMemberProfile.MinimumSupportedHeightCentimeters
            or > ParkFitMemberProfile.MaximumSupportedHeightCentimeters)
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(ParkFitSearchMemberCriteria.HeightCentimeters),
                "Member height is outside the supported range."));
        }

        ValidateAgeRange(
            member.MinimumAgeYears,
            member.MaximumAgeYears,
            nameof(ParkFitSearchMemberCriteria.MinimumAgeYears),
            errors);
        ValidateAgeRange(
            member.CompanionMinimumAgeYears,
            member.CompanionMaximumAgeYears,
            nameof(ParkFitSearchMemberCriteria.CompanionMinimumAgeYears),
            errors);
        if ((member.CompanionMinimumAgeYears.HasValue
                || member.CompanionMaximumAgeYears.HasValue)
            && member.CanBeAccompanied != true)
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(ParkFitSearchMemberCriteria.CanBeAccompanied),
                "Companion age requires explicit accompaniment availability."));
        }
    }

    private static void ValidateAgeRange(
        int? minimumYears,
        int? maximumYears,
        string field,
        ICollection<ApplicationError> errors)
    {
        if (minimumYears.HasValue != maximumYears.HasValue
            || minimumYears is < 0 or > ParkFitAgeRange.MaximumSupportedAgeYears
            || maximumYears < minimumYears
            || maximumYears > ParkFitAgeRange.MaximumSupportedAgeYears)
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                field,
                "Age ranges require valid minimum and maximum values."));
        }
    }

    private static void ValidatePreferences(
        IReadOnlyCollection<ParkItemType>? preferences,
        ICollection<ApplicationError> errors)
    {
        if (preferences is null || preferences.Count > ParkFitSearchLimits.MaximumPreferenceCount)
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(SearchParksByFitQuery.PreferredAttractionTypes),
                $"At most {ParkFitSearchLimits.MaximumPreferenceCount} attraction preferences are allowed."));
            return;
        }

        if (preferences.Distinct().Count() != preferences.Count
            || preferences.Any(static type => !Enum.IsDefined(type)
                || type is ParkItemType.Attraction or ParkItemType.Other
                || !ParkItemAdministrationDefaults.IsTypeAllowedForCategory(
                    ParkItemCategory.Attraction,
                    type)))
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(SearchParksByFitQuery.PreferredAttractionTypes),
                "Attraction preferences must be unique precise attraction types."));
        }
    }

    private static void ValidateOrigin(
        SearchParksByFitQuery request,
        ICollection<ApplicationError> errors)
    {
        bool hasLatitude = request.OriginLatitude.HasValue;
        bool hasLongitude = request.OriginLongitude.HasValue;
        if (hasLatitude != hasLongitude)
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(request.OriginLatitude),
                "Origin latitude and longitude must be provided together."));
            return;
        }

        if (!hasLatitude)
        {
            return;
        }

        double latitude = request.OriginLatitude!.Value;
        double longitude = request.OriginLongitude!.Value;
        if (!double.IsFinite(latitude) || latitude is < -90d or > 90d
            || !double.IsFinite(longitude) || longitude is < -180d or > 180d)
        {
            errors.Add(ParkFitApplicationErrors.InvalidSearch(
                nameof(request.OriginLatitude),
                "Origin coordinates are outside the supported geographic range."));
        }
    }

    private static bool IsCountryCode(string countryCode)
    {
        string normalized = countryCode.Trim();
        return normalized.Length == 2 && normalized.All(char.IsAsciiLetter);
    }
}
