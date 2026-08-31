using AmusementPark.Core.Domain.Ratings;
using MongoDB.Bson;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class RatingValueMongoExpressions
{
    public static BsonDocument BuildIsExactValidRatingValue(string fieldReference)
    {
        if (string.IsNullOrWhiteSpace(fieldReference)
            || !fieldReference.StartsWith('$'))
        {
            throw new ArgumentException("A Mongo field reference is required.", nameof(fieldReference));
        }

        double minimumValue = RatingValue.MinimumHalfSteps / 2d;
        double maximumValue = RatingValue.MaximumHalfSteps / 2d;
        BsonDocument exactHalfStepExpression = new BsonDocument("$eq", new BsonArray
        {
            new BsonDocument("$mod", new BsonArray
            {
                new BsonDocument("$multiply", new BsonArray { fieldReference, 2 }),
                1,
            }),
            0,
        });
        BsonDocument validNumericExpression = new BsonDocument("$and", new BsonArray
        {
            new BsonDocument("$gte", new BsonArray { fieldReference, minimumValue }),
            new BsonDocument("$lte", new BsonArray { fieldReference, maximumValue }),
            exactHalfStepExpression,
        });

        return new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$isNumber", fieldReference),
            validNumericExpression,
            false,
        });
    }
}
