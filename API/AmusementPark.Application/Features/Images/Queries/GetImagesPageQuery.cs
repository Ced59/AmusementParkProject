using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Queries;

/// <summary>
/// Retourne une page filtrée d'images pour l'administration.
/// </summary>
public sealed record GetImagesPageQuery(PagedQuery Paging, ImageSearchCriteria Criteria) : IQuery<ApplicationResult<PagedResult<Image>>>;
