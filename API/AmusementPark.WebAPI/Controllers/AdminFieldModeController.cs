using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/field-mode")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
public sealed class AdminFieldModeController : ControllerBase
{
    private const string CollectionName = "adminFieldModeItemProgress";
    private readonly IMongoDatabase mongoDatabase;

    public AdminFieldModeController(IMongoDatabase mongoDatabase)
    {
        this.mongoDatabase = mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase));
    }

    [HttpGet("parks/{parkId}/processed-items")]
    [ProducesResponseType(typeof(AdminFieldModeProcessedItemsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProcessedItemsAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(parkId))
        {
            return this.BadRequest();
        }

        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("parkId", parkId)
            & Builders<BsonDocument>.Filter.Eq("isProcessed", true);
        List<BsonDocument> documents = await this.Collection.Find(filter).ToListAsync(cancellationToken);
        string[] itemIds = documents
            .Select(static document => document.GetValue("itemId", BsonNull.Value).AsString)
            .Where(static itemId => !string.IsNullOrWhiteSpace(itemId))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return this.Ok(new AdminFieldModeProcessedItemsDto(parkId, itemIds));
    }

    [HttpPut("parks/{parkId}/items/{itemId}/processed")]
    [ProducesResponseType(typeof(AdminFieldModeProcessedItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetProcessedAsync(
        [FromRoute] string parkId,
        [FromRoute] string itemId,
        [FromBody] AdminFieldModeProcessedUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(parkId) || string.IsNullOrWhiteSpace(itemId))
        {
            return this.BadRequest();
        }

        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("parkId", parkId)
            & Builders<BsonDocument>.Filter.Eq("itemId", itemId);

        if (!request.IsProcessed)
        {
            await this.Collection.DeleteOneAsync(filter, cancellationToken);
            return this.Ok(new AdminFieldModeProcessedItemDto(parkId, itemId, false));
        }

        UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
            .Set("parkId", parkId)
            .Set("itemId", itemId)
            .Set("isProcessed", true)
            .Set("updatedAtUtc", DateTime.UtcNow)
            .SetOnInsert("createdAtUtc", DateTime.UtcNow);

        await this.Collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        return this.Ok(new AdminFieldModeProcessedItemDto(parkId, itemId, true));
    }

    private IMongoCollection<BsonDocument> Collection => this.mongoDatabase.GetCollection<BsonDocument>(CollectionName);
}

public sealed record AdminFieldModeProcessedItemsDto(string ParkId, IReadOnlyCollection<string> ItemIds);

public sealed record AdminFieldModeProcessedItemDto(string ParkId, string ItemId, bool IsProcessed);

public sealed record AdminFieldModeProcessedUpdateDto(bool IsProcessed);
