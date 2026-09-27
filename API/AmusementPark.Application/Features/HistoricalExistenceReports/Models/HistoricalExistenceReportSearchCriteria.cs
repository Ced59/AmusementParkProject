using AmusementPark.Application.Common.Requests;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Models;

public sealed record HistoricalExistenceReportSearchCriteria(
    PagedQuery Paging,
    HistoricalExistenceReportStatus? Status = null,
    string? ParkId = null);
