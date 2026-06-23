using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.AttractionManufacturers;
using AmusementPark.WebAPI.Contracts.Countries;
using AmusementPark.WebAPI.Contracts.ParkFounders;
using AmusementPark.WebAPI.Contracts.ParkOperators;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Helpers de mapping HTTP pour les features simples migrées en phase 6.
/// </summary>
internal static class LookupHttpMappers
{
    public static CountryDto ToHttp(this Country country, string? languageCode)
    {
        return new CountryDto
        {
            IsoCode = country.IsoCode,
            Name = country.Names.Resolve(languageCode, "en"),
        };
    }

    public static ParkFounder ToDomain(this ParkFounderCreateDto dto)
    {
        return new ParkFounder
        {
            Name = dto.Name.Trim(),
            Occupation = NormalizeOptional(dto.Occupation),
            BirthDate = NormalizeOptional(dto.BirthDate),
            DeathDate = NormalizeOptional(dto.DeathDate),
            BirthPlace = NormalizeOptional(dto.BirthPlace),
            NationalityCountryCode = NormalizeOptional(dto.NationalityCountryCode)?.ToUpperInvariant(),
            WebsiteUrl = NormalizeOptional(dto.WebsiteUrl),
            Biography = dto.Biography.ToDomain(),
        };
    }

    public static ParkFounder ToDomain(this ParkFounderUpdateDto dto)
    {
        return new ParkFounder
        {
            Name = dto.Name.Trim(),
            Occupation = NormalizeOptional(dto.Occupation),
            BirthDate = NormalizeOptional(dto.BirthDate),
            DeathDate = NormalizeOptional(dto.DeathDate),
            BirthPlace = NormalizeOptional(dto.BirthPlace),
            NationalityCountryCode = NormalizeOptional(dto.NationalityCountryCode)?.ToUpperInvariant(),
            WebsiteUrl = NormalizeOptional(dto.WebsiteUrl),
            Biography = dto.Biography.ToDomain(),
        };
    }

    public static ParkFounderDto ToHttp(this ParkFounder founder)
    {
        return new ParkFounderDto
        {
            Id = founder.Id,
            Name = founder.Name,
            Occupation = founder.Occupation,
            BirthDate = founder.BirthDate,
            DeathDate = founder.DeathDate,
            BirthPlace = founder.BirthPlace,
            NationalityCountryCode = founder.NationalityCountryCode,
            WebsiteUrl = founder.WebsiteUrl,
            Biography = founder.Biography.ToHttp(),
        };
    }

    public static ParkOperator ToDomain(this ParkOperatorCreateDto dto)
    {
        return new ParkOperator
        {
            Name = dto.Name.Trim(),
            LegalName = NormalizeOptional(dto.LegalName),
            FoundedYear = dto.FoundedYear,
            ClosedYear = dto.ClosedYear,
            ContactDetails = ToDomainContactDetails(dto.ContactDetails),
            Description = dto.Description.ToDomain(),
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
        };
    }

    public static ParkOperator ToDomain(this ParkOperatorUpdateDto dto)
    {
        return new ParkOperator
        {
            Name = dto.Name.Trim(),
            LegalName = NormalizeOptional(dto.LegalName),
            FoundedYear = dto.FoundedYear,
            ClosedYear = dto.ClosedYear,
            ContactDetails = ToDomainContactDetails(dto.ContactDetails),
            Description = dto.Description.ToDomain(),
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
        };
    }

    public static ParkOperatorDto ToHttp(this ParkOperator value)
    {
        return new ParkOperatorDto
        {
            Id = value.Id,
            Name = value.Name,
            LegalName = value.LegalName,
            FoundedYear = value.FoundedYear,
            ClosedYear = value.ClosedYear,
            ContactDetails = ToHttpContactDetails(value.ContactDetails),
            Description = value.Description.ToHttp(),
            AdminReviewStatus = value.AdminReviewStatus.ToHttp(),
        };
    }

    public static AttractionManufacturer ToDomain(this AttractionManufacturerCreateDto dto)
    {
        return new AttractionManufacturer
        {
            Name = dto.Name.Trim(),
            LegalName = NormalizeOptional(dto.LegalName),
            FoundedYear = dto.FoundedYear,
            ClosedYear = dto.ClosedYear,
            ContactDetails = ToDomainContactDetails(dto.ContactDetails),
            Biography = dto.Biography.ToDomain(),
            CurrentLogoImageId = NormalizeOptional(dto.CurrentLogoImageId),
            IsVisible = dto.IsVisible,
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
        };
    }

    public static AttractionManufacturer ToDomain(this AttractionManufacturerUpdateDto dto)
    {
        return new AttractionManufacturer
        {
            Name = dto.Name.Trim(),
            LegalName = NormalizeOptional(dto.LegalName),
            FoundedYear = dto.FoundedYear,
            ClosedYear = dto.ClosedYear,
            ContactDetails = ToDomainContactDetails(dto.ContactDetails),
            Biography = dto.Biography.ToDomain(),
            CurrentLogoImageId = NormalizeOptional(dto.CurrentLogoImageId),
            IsVisible = dto.IsVisible,
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
        };
    }

    public static AttractionManufacturerDto ToHttp(this AttractionManufacturerResult value)
    {
        return new AttractionManufacturerDto
        {
            Id = value.Id,
            Name = value.Name,
            LegalName = value.LegalName,
            FoundedYear = value.FoundedYear,
            ClosedYear = value.ClosedYear,
            ContactDetails = ToHttpContactDetails(value.ContactDetails),
            Biography = value.Biography.ToHttp(),
            CurrentLogoImageId = value.CurrentLogoImageId,
            MainImageId = value.MainImageId,
            IsVisible = value.IsVisible,
            AttractionCount = value.AttractionCount,
            AdminReviewStatus = value.AdminReviewStatus.ToHttp(),
        };
    }
    private static ParkReferenceContactDetails? ToDomainContactDetails(ParkReferenceContactDetailsDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new ParkReferenceContactDetails
        {
            WebsiteUrl = NormalizeOptional(dto.WebsiteUrl),
            Email = NormalizeOptional(dto.Email),
            PhoneNumber = NormalizeOptional(dto.PhoneNumber),
            Street = NormalizeOptional(dto.Street),
            City = NormalizeOptional(dto.City),
            PostalCode = NormalizeOptional(dto.PostalCode),
            CountryCode = NormalizeOptional(dto.CountryCode)?.ToUpperInvariant(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
        };
    }

    private static ParkReferenceContactDetailsDto? ToHttpContactDetails(ParkReferenceContactDetails? details)
    {
        if (details is null)
        {
            return null;
        }

        return new ParkReferenceContactDetailsDto
        {
            WebsiteUrl = details.WebsiteUrl,
            Email = details.Email,
            PhoneNumber = details.PhoneNumber,
            Street = details.Street,
            City = details.City,
            PostalCode = details.PostalCode,
            CountryCode = details.CountryCode,
            Latitude = details.Latitude,
            Longitude = details.Longitude,
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        return normalizedValue.Length == 0 ? null : normalizedValue;
    }

}
