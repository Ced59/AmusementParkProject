namespace AmusementPark.WebAPI.Security;

public static class ParkDataEditorAuthenticationDefaults
{
    public const string PolicyScheme = "ApiBearer";

    public const string AuthenticationScheme = "ParkDataEditorToken";

    public const string AuthenticationMethodClaim = "amusementpark:authentication_method";

    public const string AuthenticationMethod = "park-data-editor-token";

    public const string TokenIdClaim = "amusementpark:park_data_editor_token_id";

    public const string TokenLabelClaim = "amusementpark:park_data_editor_token_label";

    public const string TokenPrefix = "apf_pde_";
}
