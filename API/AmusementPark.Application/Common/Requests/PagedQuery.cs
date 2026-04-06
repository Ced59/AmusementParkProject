namespace AmusementPark.Application.Common.Requests;

/// <summary>
/// Représente une requête paginée indépendante du transport HTTP.
/// </summary>
/// <param name="Page">Numéro de page demandé.</param>
/// <param name="PageSize">Taille de page demandée.</param>
public sealed record PagedQuery(int Page = 1, int PageSize = 20);
