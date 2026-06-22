using AmusementPark.Application.Features.CaptainCoaster.Results;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

/// <summary>
/// Mappers centralisés domaine/resultats applicatifs &lt;-&gt; documents Mongo.
/// </summary>
internal static partial class EntityMongoMappers
{
    public static Country ToDomain(this CountryDocument document)
    {
        Country entity = new Country
        {
            Id = document.Id,
            IsoCode = document.IsoCode,
            Names = CommonMongoMappers.ToDomain(document.Names),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static CountryDocument ToDocument(this Country entity)
    {
        return new CountryDocument
        {
            Id = entity.Id,
            IsoCode = entity.IsoCode,
            Names = CommonMongoMappers.ToDocuments(entity.Names),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ParkFounder ToDomain(this ParkFounderDocument document)
    {
        ParkFounder entity = new ParkFounder
        {
            Id = document.Id,
            Name = document.Name,
            Occupation = document.Occupation,
            BirthDate = document.BirthDate,
            DeathDate = document.DeathDate,
            BirthPlace = document.BirthPlace,
            NationalityCountryCode = document.NationalityCountryCode,
            WebsiteUrl = document.WebsiteUrl,
            Biography = CommonMongoMappers.ToDomain(document.Biography),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkFounderDocument ToDocument(this ParkFounder entity)
    {
        return new ParkFounderDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            Occupation = entity.Occupation,
            BirthDate = entity.BirthDate,
            DeathDate = entity.DeathDate,
            BirthPlace = entity.BirthPlace,
            NationalityCountryCode = entity.NationalityCountryCode,
            WebsiteUrl = entity.WebsiteUrl,
            Biography = CommonMongoMappers.ToDocuments(entity.Biography),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ParkOperator ToDomain(this ParkOperatorDocument document)
    {
        ParkOperator entity = new ParkOperator
        {
            Id = document.Id,
            Name = document.Name,
            LegalName = document.LegalName,
            FoundedYear = document.FoundedYear,
            ClosedYear = document.ClosedYear,
            ContactDetails = ToDomainContactDetails(document.ContactDetails),
            Description = CommonMongoMappers.ToDomain(document.Description),
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkOperatorDocument ToDocument(this ParkOperator entity)
    {
        return new ParkOperatorDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            LegalName = entity.LegalName,
            FoundedYear = entity.FoundedYear,
            ClosedYear = entity.ClosedYear,
            ContactDetails = ToDocumentContactDetails(entity.ContactDetails),
            Description = CommonMongoMappers.ToDocuments(entity.Description),
            AdminReviewStatus = entity.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = entity.AdminReviewStatus.ToAdminReviewPriority(),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static AttractionManufacturer ToDomain(this AttractionManufacturerDocument document)
    {
        AttractionManufacturer entity = new AttractionManufacturer
        {
            Id = document.Id,
            Name = document.Name,
            LegalName = document.LegalName,
            FoundedYear = document.FoundedYear,
            ClosedYear = document.ClosedYear,
            ContactDetails = ToDomainContactDetails(document.ContactDetails),
            Biography = CommonMongoMappers.ToDomain(document.Biography),
            CurrentLogoImageId = document.CurrentLogoImageId,
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static AttractionManufacturerDocument ToDocument(this AttractionManufacturer entity)
    {
        return new AttractionManufacturerDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            LegalName = entity.LegalName,
            FoundedYear = entity.FoundedYear,
            ClosedYear = entity.ClosedYear,
            ContactDetails = ToDocumentContactDetails(entity.ContactDetails),
            Biography = CommonMongoMappers.ToDocuments(entity.Biography),
            CurrentLogoImageId = entity.CurrentLogoImageId,
            AdminReviewStatus = entity.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = entity.AdminReviewStatus.ToAdminReviewPriority(),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }
    private static ParkReferenceContactDetails? ToDomainContactDetails(ParkReferenceContactDetailsDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        return new ParkReferenceContactDetails
        {
            WebsiteUrl = document.WebsiteUrl,
            Email = document.Email,
            PhoneNumber = document.PhoneNumber,
            Street = document.Street,
            City = document.City,
            PostalCode = document.PostalCode,
            CountryCode = document.CountryCode,
            Latitude = document.Latitude,
            Longitude = document.Longitude,
        };
    }

    private static ParkReferenceContactDetailsDocument? ToDocumentContactDetails(ParkReferenceContactDetails? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ParkReferenceContactDetailsDocument
        {
            WebsiteUrl = entity.WebsiteUrl,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            Street = entity.Street,
            City = entity.City,
            PostalCode = entity.PostalCode,
            CountryCode = entity.CountryCode,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
        };
    }

}
