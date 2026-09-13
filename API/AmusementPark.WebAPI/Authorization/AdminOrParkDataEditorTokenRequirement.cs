using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Authorization;

public sealed class AdminOrParkDataEditorTokenRequirement : IAuthorizationRequirement
{
    public static AdminOrParkDataEditorTokenRequirement Instance { get; } = new AdminOrParkDataEditorTokenRequirement();

    private AdminOrParkDataEditorTokenRequirement()
    {
    }
}
