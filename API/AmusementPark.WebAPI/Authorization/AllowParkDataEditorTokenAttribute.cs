namespace AmusementPark.WebAPI.Authorization;

/// <summary>
/// Marque explicitement une surface HTTP comme accessible par un jeton technique
/// PARK_DATA_EDITOR. Sans ce marqueur, la policy par défaut refuse ces jetons.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AllowParkDataEditorTokenAttribute : Attribute
{
}
