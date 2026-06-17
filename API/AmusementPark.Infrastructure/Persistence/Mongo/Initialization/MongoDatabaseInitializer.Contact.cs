using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Contact;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeContactGrievanceIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ContactGrievanceDocument> collection = this.database.GetCollection<ContactGrievanceDocument>(this.settings.ContactGrievancesCollectionName);
        List<CreateIndexModel<ContactGrievanceDocument>> indexes = new List<CreateIndexModel<ContactGrievanceDocument>>
        {
            new CreateIndexModel<ContactGrievanceDocument>(
                Builders<ContactGrievanceDocument>.IndexKeys.Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_contact_grievances_created_desc" }),
            new CreateIndexModel<ContactGrievanceDocument>(
                Builders<ContactGrievanceDocument>.IndexKeys
                    .Ascending(static document => document.IpAddress)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_contact_grievances_ip_created_desc" }),
            new CreateIndexModel<ContactGrievanceDocument>(
                Builders<ContactGrievanceDocument>.IndexKeys
                    .Ascending(static document => document.LanguageCode)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_contact_grievances_language_created_desc" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
