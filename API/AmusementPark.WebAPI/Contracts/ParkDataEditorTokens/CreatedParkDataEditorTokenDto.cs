using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.ParkDataEditorTokens;

public sealed class CreatedParkDataEditorTokenDto
{
    public ParkDataEditorTokenDto Token { get; set; } = new ParkDataEditorTokenDto();

    /// <summary>
    /// Secret retourné une seule fois. Il ne peut pas être relu ultérieurement.
    /// </summary>
    public string PlainTextToken { get; set; } = string.Empty;
}
