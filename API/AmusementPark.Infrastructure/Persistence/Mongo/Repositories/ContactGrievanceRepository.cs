using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Contact.Contracts;
using AmusementPark.Application.Features.Contact.Ports;
using AmusementPark.Core.Domain.Contact;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Contact;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ContactGrievanceRepository : IContactGrievanceRepository
{
    private readonly IMongoCollection<ContactGrievanceDocument> collection;

    public ContactGrievanceRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ContactGrievanceDocument>(settings.ContactGrievancesCollectionName);
    }

    public async Task<ContactGrievance> CreateAsync(ContactGrievance grievance, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        grievance.Id = Guid.NewGuid().ToString("N");
        grievance.CreatedAtUtc = now;
        grievance.UpdatedAtUtc = now;

        ContactGrievanceDocument document = grievance.ToDocument();
        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public Task<long> CountRecentByIpAsync(string ipAddress, DateTime submittedSinceUtc, CancellationToken cancellationToken)
    {
        FilterDefinition<ContactGrievanceDocument> filter = Builders<ContactGrievanceDocument>.Filter.Eq(static document => document.IpAddress, ipAddress)
            & Builders<ContactGrievanceDocument>.Filter.Gte(static document => document.CreatedAt, submittedSinceUtc);

        return this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<ContactGrievance>> GetPageAsync(int page, int pageSize, ContactGrievanceSearchCriteria criteria, CancellationToken cancellationToken)
    {
        int safePage = Math.Max(1, page);
        int safePageSize = Math.Clamp(pageSize, 1, 100);
        FilterDefinition<ContactGrievanceDocument> filter = BuildFilter(criteria);
        SortDefinition<ContactGrievanceDocument> sort = Builders<ContactGrievanceDocument>.Sort.Descending(static document => document.CreatedAt);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<ContactGrievanceDocument> documents = await this.collection
            .Find(filter)
            .Sort(sort)
            .Skip((safePage - 1) * safePageSize)
            .Limit(safePageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<ContactGrievance> items = documents.Select(static document => document.ToDomain()).ToList();
        return new PagedResult<ContactGrievance>(items, safePage, safePageSize, totalItems);
    }

    private static FilterDefinition<ContactGrievanceDocument> BuildFilter(ContactGrievanceSearchCriteria criteria)
    {
        FilterDefinitionBuilder<ContactGrievanceDocument> builder = Builders<ContactGrievanceDocument>.Filter;
        FilterDefinition<ContactGrievanceDocument> filter = builder.Empty;

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            BsonRegularExpression regex = new BsonRegularExpression(Regex.Escape(criteria.Search.Trim()), "i");
            filter &= builder.Regex(static document => document.Message, regex);
        }

        if (!string.IsNullOrWhiteSpace(criteria.IpAddress))
        {
            filter &= builder.Eq(static document => document.IpAddress, criteria.IpAddress.Trim());
        }

        if (!string.IsNullOrWhiteSpace(criteria.LanguageCode))
        {
            filter &= builder.Eq(static document => document.LanguageCode, criteria.LanguageCode.Trim().ToLowerInvariant());
        }

        return filter;
    }
}
