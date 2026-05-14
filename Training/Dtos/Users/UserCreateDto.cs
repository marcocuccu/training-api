using System.ComponentModel.DataAnnotations;

namespace Training.Dtos;

public partial class UserCreateDto
{
    [Required]
    public string FirstName { get; set; }
    
    [Required]
    public string LastName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    public string? Gender { get; set; }
}