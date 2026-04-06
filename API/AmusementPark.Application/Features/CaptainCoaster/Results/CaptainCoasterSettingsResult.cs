namespace AmusementPark.Application.Features.CaptainCoaster.Results;

/// <summary>
/// Représente les paramètres applicatifs Captain Coaster.
/// </summary>
public sealed class CaptainCoasterSettingsResult
{
    public bool IsEnabled { get; init; }

    public string? DataDirectoryPath { get; init; }

    public string? HtmlDirectoryPath { get; init; }

    public bool UseOfflineMode { get; init; }
}
