using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

/// <summary>
/// Expose l'état runtime de génération au panneau admin.
/// </summary>
public interface ISeoSitemapRuntimeStateStore
{
    SitemapRuntimeState GetCurrent();

    bool TryStart(string step);

    void Update(string step, int progressPercentage, string? message = null);

    void Complete(string step, string? message = null);

    void Fail(string step, string message);
}
