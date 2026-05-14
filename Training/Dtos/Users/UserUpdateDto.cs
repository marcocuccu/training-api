using System.ComponentModel.DataAnnotations;

namespace Training.Dtos;

public partial class UserUpdateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    [EmailAddress] public string? Email { get; set; }
    public string? Gender { get; set; }
}