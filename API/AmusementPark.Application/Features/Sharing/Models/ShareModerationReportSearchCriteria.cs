using AmusementPark.Application.Common.Requests;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record ShareModerationReportSearchCriteria(
    PagedQuery Paging,
    ShareModerationReportStatus? Status = null,
    ShareModerationTargetType? TargetType = null,
    ShareModerationReason? Reason = null);
