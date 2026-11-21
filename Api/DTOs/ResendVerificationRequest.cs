using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = "";
}
