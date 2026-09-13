using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Queries;

public sealed record GetShareModerationReportsQuery(
    ShareModerationReportSearchCriteria Criteria)
    : IQuery<ApplicationResult<PagedResult<ShareModerationReportResult>>>;
