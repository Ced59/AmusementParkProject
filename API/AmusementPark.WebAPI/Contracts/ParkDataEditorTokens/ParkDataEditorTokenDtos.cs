using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.ParkDataEditorTokens;

public sealed class CreateParkDataEditorTokenRequestDto
{
    [Required]
    [StringLength(80, MinimumLength = 3)]
    public string Label { get; set; } = string.Empty;

    [Range(1, 90)]
    public int ExpiresInDays { get; set; } = 30;
}

public sealed class ParkDataEditorTokenDto
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string DisplayPrefix { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? RevokedByUserId { get; set; }

    public string? RevocationReason { get; set; }

    public bool IsActive { get; set; }
}

public sealed class CreatedParkDataEditorTokenDto
{
    public ParkDataEditorTokenDto Token { get; set; } = new ParkDataEditorTokenDto();

    /// <summary>
    /// Secret retourné une seule fois. Il ne peut pas être relu ultérieurement.
    /// </summary>
    public string PlainTextToken { get; set; } = string.Empty;
}

public sealed class RevokedParkDataEditorTokensDto
{
    public long RevokedCount { get; set; }
}
