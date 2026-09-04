using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

public static class PassportVisitDeletionHttpMapper
{
    public static PassportVisitDeletionPreviewDto ToHttp(this VisitDeletionPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new PassportVisitDeletionPreviewDto
        {
            VisitId = preview.VisitId,
            ExpectedVersion = preview.ExpectedVersion,
            OccurrenceCount = preview.OccurrenceCount,
            AssessmentCount = preview.AssessmentCount,
            RetentionDays = preview.RetentionDays,
        };
    }

    public static DeleteVisitCommand ToApplication(
        this DeletePassportVisitRequestDto request,
        string userId,
        string visitId,
        string clientOperationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new DeleteVisitCommand(
            userId,
            visitId,
            request.ExpectedVersion,
            request.ConfirmedOccurrenceCount,
            request.ConfirmedAssessmentCount,
            clientOperationId);
    }

    public static PassportVisitDeletionReceiptDto ToHttp(this VisitDeletionReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return new PassportVisitDeletionReceiptDto
        {
            VisitId = receipt.VisitId,
            DeletedAtUtc = receipt.DeletedAtUtc,
            PurgeScheduledForUtc = receipt.PurgeScheduledForUtc,
        };
    }
}
