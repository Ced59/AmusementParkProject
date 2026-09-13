using AmusementPark.WebAPI.Services;

namespace AmusementPark.WebAPI.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SkipParkDataEditorOperationCoordinationAttribute : Attribute
{
}
