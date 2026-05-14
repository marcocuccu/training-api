using System.ComponentModel.DataAnnotations;

namespace Training.Dtos;

public partial class RegisterRequestDto
{
    [Required]
    public string FirstName { get; set; }
    
    [Required]
    public string LastName { get; set; }
    
    public string? Gender { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [MinLength(8)]
    public string Password { get; set; }

    [Required]
    [MinLength(8)]
    [Compare("Password")]
    public string PasswordConfirmation { get; set; }

    public RegisterRequestDto()
    {
        FirstName ??= "";
        LastName ??= "";
        Email ??= "";
        Password ??= "";
        PasswordConfirmation ??= "";
    }
}