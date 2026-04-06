using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Parks;
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
            Biography = dto.Biography.ToDomain(),
        };
    }

    public static ParkFounder ToDomain(this ParkFounderUpdateDto dto)
    {
        return new ParkFounder
        {
            Name = dto.Name.Trim(),
            Biography = dto.Biography.ToDomain(),
        };
    }

    public static ParkFounderDto ToHttp(this ParkFounder founder)
    {
        return new ParkFounderDto
        {
            Id = founder.Id,
            Name = founder.Name,
            Biography = founder.Biography.ToHttp(),
        };
    }

    public static ParkOperator ToDomain(this ParkOperatorCreateDto dto)
    {
        return new ParkOperator
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.ToDomain(),
        };
    }

    public static ParkOperator ToDomain(this ParkOperatorUpdateDto dto)
    {
        return new ParkOperator
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.ToDomain(),
        };
    }

    public static ParkOperatorDto ToHttp(this ParkOperator value)
    {
        return new ParkOperatorDto
        {
            Id = value.Id,
            Name = value.Name,
            Description = value.Description.ToHttp(),
        };
    }

    public static AttractionManufacturer ToDomain(this AttractionManufacturerCreateDto dto)
    {
        return new AttractionManufacturer
        {
            Name = dto.Name.Trim(),
            Biography = dto.Biography.ToDomain(),
        };
    }

    public static AttractionManufacturer ToDomain(this AttractionManufacturerUpdateDto dto)
    {
        return new AttractionManufacturer
        {
            Name = dto.Name.Trim(),
            Biography = dto.Biography.ToDomain(),
        };
    }

    public static AttractionManufacturerDto ToHttp(this AttractionManufacturerResult value)
    {
        return new AttractionManufacturerDto
        {
            Id = value.Id,
            Name = value.Name,
            Biography = value.Biography.ToHttp(),
            AttractionCount = value.AttractionCount,
        };
    }
}
