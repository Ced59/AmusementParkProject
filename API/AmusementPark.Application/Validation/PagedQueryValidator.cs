using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Validation;

/// <summary>
/// Valide les paramètres paginés communs.
/// </summary>
public sealed class PagedQueryValidator : IApplicationValidator<PagedQuery>
{
    /// <inheritdoc />
    public IReadOnlyCollection<ApplicationError> Validate(PagedQuery request)
    {
        if (request.Page <= 0 || request.PageSize <= 0)
        {
            return new[] { ApplicationErrors.InvalidPagination() };
        }

        return Array.Empty<ApplicationError>();
    }
}
