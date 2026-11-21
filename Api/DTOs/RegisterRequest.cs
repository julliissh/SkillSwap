using System.ComponentModel.DataAnnotations;
using Common.Entities;

namespace Api.DTOs;

public class RegisterRequest
{
    [Required]
    [MinLength(Profile.MinNameLength)]
    [MaxLength(Profile.MaxNameLength)]
    public string FirstName { get; set; } = "";

    [Required]
    [MinLength(Profile.MinNameLength)]
    [MaxLength(Profile.MaxNameLength)]
    public string LastName { get; set; } = "";

    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = "";
}
