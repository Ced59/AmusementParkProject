using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.Users;

public sealed class DeleteAccountRequestDto
{
    [Required]
    [EmailAddress]
    public string ConfirmationEmail { get; set; } = string.Empty;

    public string CurrentPassword { get; set; } = string.Empty;
}
