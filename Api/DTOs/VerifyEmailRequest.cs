using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class VerifyEmailRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = "";

    [Required]
    [RegularExpression(@"^[0-9]{6}$")]
    public string Code { get; set; } = "";
}
