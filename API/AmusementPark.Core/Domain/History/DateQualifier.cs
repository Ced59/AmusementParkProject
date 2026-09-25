namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Qualification éditoriale d'une date historique sans précision calendaire inventée.
/// </summary>
public enum DateQualifier
{
    Early = 1,
    Mid = 2,
    Late = 3,
    Before = 4,
    After = 5,
    Circa = 6,
}
