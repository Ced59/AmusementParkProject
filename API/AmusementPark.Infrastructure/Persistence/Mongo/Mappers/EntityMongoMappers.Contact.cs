using AmusementPark.Core.Domain.Contact;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Contact;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static ContactGrievance ToDomain(this ContactGrievanceDocument document)
    {
        ContactGrievance entity = new ContactGrievance
        {
            Id = document.Id,
            Message = document.Message,
            LanguageCode = document.LanguageCode,
            IpAddress = document.IpAddress,
            UserAgent = document.UserAgent,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ContactGrievanceDocument ToDocument(this ContactGrievance entity)
    {
        return new ContactGrievanceDocument
        {
            Id = entity.Id,
            Message = entity.Message,
            LanguageCode = entity.LanguageCode,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }
}
