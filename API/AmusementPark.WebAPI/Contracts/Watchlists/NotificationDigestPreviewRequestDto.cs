using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class NotificationDigestPreviewRequestDto
{
    [Required]
    [StringLength(200)]
    public string UserId { get; init; } = string.Empty;

    [Required]
    public string Frequency { get; init; } = string.Empty;

    public DateTime PeriodStartUtc { get; init; }
}
