using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.ParkDataEditorTokens;

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
