using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = "";
}
