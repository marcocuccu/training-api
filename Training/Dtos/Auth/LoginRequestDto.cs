using System.ComponentModel.DataAnnotations;

namespace Training.Dtos;

public partial class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [MinLength(8)]
    public string Password { get; set; }

    public LoginRequestDto()
    {
        Email ??= "";
        Password ??= "";
    }
}