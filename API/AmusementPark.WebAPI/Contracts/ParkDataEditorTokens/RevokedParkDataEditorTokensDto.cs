using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.ParkDataEditorTokens;

public sealed class RevokedParkDataEditorTokensDto
{
    public long RevokedCount { get; set; }
}
