using AmusementPark.Application.Common.Requests;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Models;

public sealed record ParkFitSourceReportSearchCriteria(
    PagedQuery Paging,
    ParkFitSourceReportStatus? Status = null,
    ParkFitSourceReportReason? Reason = null,
    string? ParkId = null);
