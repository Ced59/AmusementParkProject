using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Initialization;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Initialise les collections MongoDB métier, leurs index et les seeds de base.
/// </summary>
public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeRefreshTokensIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<RefreshTokenDocument> collection = this.database.GetCollection<RefreshTokenDocument>(this.settings.RefreshTokensCollectionName);

        List<CreateIndexModel<RefreshTokenDocument>> indexes = new List<CreateIndexModel<RefreshTokenDocument>>
        {
            new CreateIndexModel<RefreshTokenDocument>(
                Builders<RefreshTokenDocument>.IndexKeys.Ascending(item => item.TokenHash),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_refresh_tokens_tokenHash",
                }),
            new CreateIndexModel<RefreshTokenDocument>(
                Builders<RefreshTokenDocument>.IndexKeys.Ascending(item => item.UserId).Descending(item => item.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "ix_refresh_tokens_userId_expiresAtUtc",
                }),
            new CreateIndexModel<RefreshTokenDocument>(
                Builders<RefreshTokenDocument>.IndexKeys.Ascending(item => item.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "ix_refresh_tokens_expiresAtUtc_ttl",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task InitializeAdminUserAsync(CancellationToken cancellationToken)
    {
        if (!this.adminSeedSettings.Enabled)
        {
            this.logger.LogDebug("Admin seed is disabled.");
            return;
        }

        if (!this.hostEnvironment.IsDevelopment())
        {
            this.logger.LogWarning("Admin seed is enabled but ignored because the current environment is {EnvironmentName}. Admin seed is reserved for local development.", this.hostEnvironment.EnvironmentName);
            return;
        }

        if (string.IsNullOrWhiteSpace(this.adminSeedSettings.Email))
        {
            this.logger.LogWarning("Admin seed ignored because Initialization:AdminUser:Email is empty.");
            return;
        }

        IMongoCollection<UserDocument> usersCollection = this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        string normalizedEmail = this.adminSeedSettings.Email.Trim().ToLowerInvariant();

        UserDocument? existingUser = await usersCollection
            .Find(user => user.Email == normalizedEmail)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser is null)
        {
            if (string.IsNullOrWhiteSpace(this.adminSeedSettings.Password))
            {
                this.logger.LogWarning("Admin seed ignored because Initialization:AdminUser:Password is empty. Configure it through user-secrets or environment variables when local admin seeding is required.");
                return;
            }

            DateTime now = DateTime.UtcNow;
            UserDocument adminUser = new UserDocument
            {
                Email = normalizedEmail,
                FirstName = string.IsNullOrWhiteSpace(this.adminSeedSettings.FirstName) ? "Admin" : this.adminSeedSettings.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(this.adminSeedSettings.LastName) ? "User" : this.adminSeedSettings.LastName.Trim(),
                PreferredLanguage = string.IsNullOrWhiteSpace(this.adminSeedSettings.PreferredLanguage)
                    ? "FR"
                    : this.adminSeedSettings.PreferredLanguage.Trim().ToUpperInvariant(),
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(this.adminSeedSettings.Password),
                IsActivated = true,
                IsBlocked = false,
                Roles = new List<Role>
                {
                    Role.User,
                    Role.Moderator,
                    Role.Admin,
                },
                CreatedAt = now,
                UpdatedAt = now,
                LastLoginUtc = now,
                LastActivityUtc = now,
            };

            await usersCollection.InsertOneAsync(adminUser, cancellationToken: cancellationToken);
            return;
        }

        bool needsUpdate = false;

        if (!existingUser.IsActivated)
        {
            existingUser.IsActivated = true;
            needsUpdate = true;
        }

        if (existingUser.IsBlocked)
        {
            existingUser.IsBlocked = false;
            needsUpdate = true;
        }

        foreach (Role role in new[] { Role.User, Role.Moderator, Role.Admin })
        {
            if (!existingUser.Roles.Contains(role))
            {
                existingUser.Roles.Add(role);
                needsUpdate = true;
            }
        }

        if (string.IsNullOrWhiteSpace(existingUser.HashedPassword) && !string.IsNullOrWhiteSpace(this.adminSeedSettings.Password))
        {
            existingUser.HashedPassword = BCrypt.Net.BCrypt.HashPassword(this.adminSeedSettings.Password);
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            existingUser.UpdatedAt = DateTime.UtcNow;
            await usersCollection.ReplaceOneAsync(user => user.Id == existingUser.Id, existingUser, cancellationToken: cancellationToken);
        }
    }

    private async Task SeedCountriesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CountryDocument> countriesCollection = this.database.GetCollection<CountryDocument>(this.settings.CountriesCollectionName);
        long documentCount = await countriesCollection.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);

        if (documentCount > 0)
        {
            return;
        }

        string jsonFilePath = this.ResolveCountriesSeedPath();
        if (!File.Exists(jsonFilePath))
        {
            this.logger.LogWarning("Countries seed JSON file was not found at path {JsonFilePath}.", jsonFilePath);
            return;
        }

        string json = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
        };

        List<CountrySeedItem>? seedItems = JsonSerializer.Deserialize<List<CountrySeedItem>>(json, options);
        if (seedItems is null || seedItems.Count == 0)
        {
            this.logger.LogWarning("Countries seed JSON file did not contain any deserializable country.");
            return;
        }

        List<CountryDocument> countries = seedItems.Select(static item => new CountryDocument
        {
            Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("D") : item.Id,
            IsoCode = item.IsoCode?.Trim().ToUpperInvariant() ?? string.Empty,
            Names = item.Names.Select(static name => new LocalizedTextDocument
            {
                LanguageCode = name.LanguageCode?.Trim().ToLowerInvariant() ?? string.Empty,
                Value = name.Value,
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        }).ToList();

        if (countries.Count > 0)
        {
            await countriesCollection.InsertManyAsync(countries, cancellationToken: cancellationToken);
        }
    }

    private string ResolveCountriesSeedPath()
    {
        string contentRootPath = this.hostEnvironment.ContentRootPath;
        string primaryPath = Path.Combine(contentRootPath, "Resources", "InitializingDatas", "countries.seed.json");
        if (File.Exists(primaryPath))
        {
            return primaryPath;
        }

        string fallbackPath = Path.Combine(AppContext.BaseDirectory, "Resources", "InitializingDatas", "countries.seed.json");
        return fallbackPath;
    }

    private async Task InitializeUsersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserDocument> usersCollection = this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        List<CreateIndexModel<UserDocument>> indexes = new List<CreateIndexModel<UserDocument>>
        {
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(item => item.Email),
                new CreateIndexOptions { Name = "idx_users_email_unique", Unique = true }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Descending(item => item.LastActivityUtc),
                new CreateIndexOptions { Name = "idx_users_last_activity_desc" }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys
                    .Ascending("externalLogins.provider")
                    .Ascending("externalLogins.providerUserId"),
                new CreateIndexOptions { Name = "idx_users_external_login" }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(item => item.EmailConfirmationTokenHash),
                new CreateIndexOptions<UserDocument>
                {
                    Name = "idx_users_email_confirmation_token_hash",
                    PartialFilterExpression = Builders<UserDocument>.Filter.Type(item => item.EmailConfirmationTokenHash, BsonType.String),
                }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(item => item.PasswordResetTokenHash),
                new CreateIndexOptions<UserDocument>
                {
                    Name = "idx_users_password_reset_token_hash",
                    PartialFilterExpression = Builders<UserDocument>.Filter.Type(item => item.PasswordResetTokenHash, BsonType.String),
                }),
        };

        await usersCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task BackfillPublicAccountIdentitiesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserDocument> usersCollection =
            this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        FilterDefinition<UserDocument> migrationFilter = BuildPublicIdentityMigrationFilter();
        ProjectionDefinition<UserDocument> migrationProjection = Builders<UserDocument>.Projection
            .Include(document => document.Id)
            .Include(document => document.PublicAccountNumber)
            .Include(document => document.PublicDisplayName)
            .Include(document => document.UsesAutomaticPublicDisplayName)
            .Include(document => document.Roles)
            .Include(document => document.CreatedAt);
        List<UserDocument> users = await usersCollection
            .Find(migrationFilter)
            .Project<UserDocument>(migrationProjection)
            .ToListAsync(cancellationToken);
        if (users.Count == 0)
        {
            return;
        }

        List<long> existingNumbers = await usersCollection
            .Find(Builders<UserDocument>.Filter.Gt(document => document.PublicAccountNumber, 0))
            .Project(document => document.PublicAccountNumber)
            .ToListAsync(cancellationToken);
        HashSet<long> reservedNumbers = existingNumbers
            .ToHashSet();
        long nextNumber = 1;
        long modifiedCount = 0;

        foreach (UserDocument user in users
            .OrderBy(static user => ResolvePublicIdentityRoleOrder(user.Roles))
            .ThenBy(static user => user.CreatedAt)
            .ThenBy(static user => user.Id, StringComparer.Ordinal))
        {
            bool needsNumber = user.PublicAccountNumber <= 0;
            if (needsNumber)
            {
                while (!reservedNumbers.Add(nextNumber))
                {
                    nextNumber++;
                }

                user.PublicAccountNumber = nextNumber;
                nextNumber++;
            }

            bool needsAutomaticDisplayName = string.IsNullOrWhiteSpace(user.PublicDisplayName)
                || user.UsesAutomaticPublicDisplayName;
            string? publicDisplayName = needsAutomaticDisplayName
                ? PublicDisplayNameFactory.Create(user.Roles, user.PublicAccountNumber)
                : user.PublicDisplayName?.Trim();
            bool needsUpdate = needsNumber
                || !string.Equals(user.PublicDisplayName, publicDisplayName, StringComparison.Ordinal)
                || (needsAutomaticDisplayName && !user.UsesAutomaticPublicDisplayName);
            if (!needsUpdate)
            {
                continue;
            }

            UpdateResult result = await usersCollection.UpdateOneAsync(
                Builders<UserDocument>.Filter.Eq(document => document.Id, user.Id),
                Builders<UserDocument>.Update
                    .Set(document => document.PublicAccountNumber, user.PublicAccountNumber)
                    .Set(document => document.PublicDisplayName, publicDisplayName)
                    .Set(document => document.UsesAutomaticPublicDisplayName, needsAutomaticDisplayName)
                    .Set(document => document.UpdatedAt, DateTime.UtcNow),
                cancellationToken: cancellationToken);
            modifiedCount += result.ModifiedCount;
        }

        long maximumNumber = reservedNumbers.Count == 0 ? 0 : reservedNumbers.Max();
        IMongoCollection<PublicAccountCounterDocument> countersCollection =
            this.database.GetCollection<PublicAccountCounterDocument>(this.settings.CountersCollectionName);
        await countersCollection.UpdateOneAsync(
            Builders<PublicAccountCounterDocument>.Filter.Eq(
                document => document.Id,
                "public-account-number"),
            Builders<PublicAccountCounterDocument>.Update.Max(
                document => document.Sequence,
                maximumNumber),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);

        this.logger.LogInformation(
            "Backfilled a stable public account identity for {Count} existing user accounts.",
            modifiedCount);
    }

    internal static FilterDefinition<UserDocument> BuildPublicIdentityMigrationFilter()
    {
        FilterDefinitionBuilder<UserDocument> builder = Builders<UserDocument>.Filter;
        return builder.Or(
            builder.Exists(document => document.PublicAccountNumber, false),
            builder.Lte(document => document.PublicAccountNumber, 0),
            builder.Exists(document => document.PublicDisplayName, false),
            builder.Eq(document => document.PublicDisplayName, null),
            builder.Eq(document => document.PublicDisplayName, string.Empty),
            builder.Exists(document => document.UsesAutomaticPublicDisplayName, false));
    }

    private async Task InitializeUserPublicDisplayNameIndexAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserDocument> usersCollection =
            this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        CreateIndexModel<UserDocument>[] indexes =
        {
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(document => document.PublicDisplayName),
                new CreateIndexOptions<UserDocument>
                {
                    Name = "idx_users_public_display_name_unique",
                    Unique = true,
                    Collation = new Collation("en", strength: CollationStrength.Secondary),
                    PartialFilterExpression = Builders<UserDocument>.Filter.Type(
                        document => document.PublicDisplayName,
                        BsonType.String),
                }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(document => document.PublicAccountNumber),
                new CreateIndexOptions
                {
                    Name = "idx_users_public_account_number_unique",
                    Unique = true,
                }),
        };

        await usersCollection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task BackfillLegacyCommentAuthorSnapshotsAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CommentDocument> commentsCollection =
            this.database.GetCollection<CommentDocument>(this.settings.CommentsCollectionName);
        List<CommentDocument> legacyComments = await commentsCollection
            .Find(BuildLegacyCommentAuthorSnapshotFilter())
            .Project<CommentDocument>(
                Builders<CommentDocument>.Projection
                    .Include(document => document.Id)
                    .Include(document => document.AuthorUserId)
                    .Include(document => document.UpdatedAt))
            .ToListAsync(cancellationToken);
        if (legacyComments.Count == 0)
        {
            return;
        }

        string[] authorUserIds = legacyComments
            .Select(static comment => comment.AuthorUserId)
            .Where(static authorUserId => !string.IsNullOrWhiteSpace(authorUserId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IMongoCollection<UserDocument> usersCollection =
            this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        List<UserDocument> authors = await usersCollection
            .Find(Builders<UserDocument>.Filter.In(document => document.Id, authorUserIds))
            .Project<UserDocument>(
                Builders<UserDocument>.Projection
                    .Include(document => document.Id)
                    .Include(document => document.PublicDisplayName)
                    .Include(document => document.PublicAccountNumber)
                    .Include(document => document.Roles)
                    .Include(document => document.AvatarUrl))
            .ToListAsync(cancellationToken);
        Dictionary<string, UserDocument> authorsById = authors.ToDictionary(
            static author => author.Id,
            StringComparer.Ordinal);
        List<WriteModel<CommentDocument>> writes = new List<WriteModel<CommentDocument>>(legacyComments.Count);

        foreach (CommentDocument comment in legacyComments)
        {
            authorsById.TryGetValue(comment.AuthorUserId, out UserDocument? author);
            writes.Add(new UpdateOneModel<CommentDocument>(
                Builders<CommentDocument>.Filter.Eq(document => document.Id, comment.Id),
                BuildLegacyCommentAuthorSnapshotUpdate(comment, author)));
        }

        BulkWriteResult<CommentDocument> result = await commentsCollection.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
        this.logger.LogInformation(
            "Migrated {Count} legacy comment author snapshots to public pseudonyms.",
            result.ModifiedCount);
    }

    internal static FilterDefinition<CommentDocument> BuildLegacyCommentAuthorSnapshotFilter()
    {
        FilterDefinitionBuilder<CommentDocument> builder = Builders<CommentDocument>.Filter;
        FilterDefinition<CommentDocument> missingPublicDisplayName = builder.Or(
            builder.Exists(document => document.AuthorDisplayName, false),
            builder.Eq(document => document.AuthorDisplayName, null),
            builder.Eq(document => document.AuthorDisplayName, string.Empty));
        return builder.Or(
            builder.Exists("authorDisplayName", true),
            missingPublicDisplayName);
    }

    internal static UpdateDefinition<CommentDocument> BuildLegacyCommentAuthorSnapshotUpdate(
        CommentDocument comment,
        UserDocument? author)
    {
        string publicDisplayName = ResolveLegacyCommentAuthorPublicDisplayName(author);
        return Builders<CommentDocument>.Update
            .Set(document => document.AuthorDisplayName, publicDisplayName)
            .Set(document => document.AuthorAvatarUrl, author?.AvatarUrl)
            .Set(document => document.UpdatedAt, comment.UpdatedAt)
            .Unset("authorDisplayName");
    }

    internal static string ResolveLegacyCommentAuthorPublicDisplayName(UserDocument? author)
    {
        string publicDisplayName = author?.PublicDisplayName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(publicDisplayName)
            && author is not null
            && author.PublicAccountNumber > 0)
        {
            publicDisplayName = PublicDisplayNameFactory.Create(
                author.Roles,
                author.PublicAccountNumber);
        }

        return string.IsNullOrWhiteSpace(publicDisplayName)
            ? "User"
            : publicDisplayName;
    }

    private static int ResolvePublicIdentityRoleOrder(IReadOnlyCollection<Role> roles)
    {
        if (roles.Contains(Role.Admin))
        {
            return 0;
        }

        return roles.Contains(Role.Moderator) ? 1 : 2;
    }

    private async Task InitializeCountriesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CountryDocument> countriesCollection = this.database.GetCollection<CountryDocument>(this.settings.CountriesCollectionName);
        List<CreateIndexModel<CountryDocument>> indexes = new List<CreateIndexModel<CountryDocument>>
        {
            new CreateIndexModel<CountryDocument>(
                Builders<CountryDocument>.IndexKeys.Ascending(item => item.IsoCode),
                new CreateIndexOptions { Name = "idx_countries_iso_code_unique", Unique = true }),
        };

        await countriesCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private sealed class CountrySeedItem
    {
        [JsonPropertyName("_id")]
        public string? Id { get; set; }

        [JsonPropertyName("isoCode")]
        public string? IsoCode { get; set; }

        [JsonPropertyName("names")]
        public List<LocalizedTextSeedItem> Names { get; set; } = new List<LocalizedTextSeedItem>();
    }

    private sealed class LocalizedTextSeedItem
    {
        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
