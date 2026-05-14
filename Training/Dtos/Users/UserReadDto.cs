using System.ComponentModel.DataAnnotations;

namespace Training.Dtos;

public partial class UserReadDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    [EmailAddress] public string Email { get; set; }
    public string? Gender { get; set; }
}