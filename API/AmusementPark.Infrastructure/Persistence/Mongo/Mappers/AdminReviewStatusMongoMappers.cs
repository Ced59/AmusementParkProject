using AmusementPark.Core.Domain.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

/// <summary>
/// Helpers Mongo pour le statut de revue admin.
/// </summary>
internal static class AdminReviewStatusMongoMappers
{
    public static AdminReviewStatus NormalizeForAdministration(this AdminReviewStatus status)
    {
        return status switch
        {
            AdminReviewStatus.Validated => AdminReviewStatus.Validated,
            AdminReviewStatus.ToProcessLater => AdminReviewStatus.ToProcessLater,
            AdminReviewStatus.NotRelevant => AdminReviewStatus.NotRelevant,
            _ => AdminReviewStatus.ToReview,
        };
    }

    public static int ToAdminReviewPriority(this AdminReviewStatus status)
    {
        return status.NormalizeForAdministration() switch
        {
            AdminReviewStatus.Validated => 10,
            AdminReviewStatus.ToProcessLater => 90,
            AdminReviewStatus.NotRelevant => 99,
            _ => 0,
        };
    }

    public static FilterDefinition<TDocument> BuildAdminReviewStatusFilter<TDocument>(
        this FilterDefinitionBuilder<TDocument> builder,
        string statusFieldName,
        AdminReviewStatus adminReviewStatus)
    {
        AdminReviewStatus normalizedStatus = adminReviewStatus.NormalizeForAdministration();
        if (normalizedStatus == AdminReviewStatus.ToReview)
        {
            return builder.Or(
                builder.Eq(statusFieldName, AdminReviewStatus.ToReview.ToString()),
                builder.Exists(statusFieldName, false));
        }

        if (normalizedStatus == AdminReviewStatus.Validated)
        {
            return builder.Or(
                builder.Eq(statusFieldName, AdminReviewStatus.Validated.ToString()),
                builder.Eq(statusFieldName, "Ready"));
        }

        return builder.Eq(statusFieldName, normalizedStatus.ToString());
    }
}
