using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Authorization;

public sealed class RestrictedParkDataEditorTokenRequirement : IAuthorizationRequirement
{
    public static RestrictedParkDataEditorTokenRequirement Instance { get; } = new RestrictedParkDataEditorTokenRequirement();

    private RestrictedParkDataEditorTokenRequirement()
    {
    }
}
