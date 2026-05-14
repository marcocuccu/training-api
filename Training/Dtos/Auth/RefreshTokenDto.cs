using System.ComponentModel.DataAnnotations;

namespace Training.Dtos;

public partial class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; }
}