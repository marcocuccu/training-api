using System.ComponentModel.DataAnnotations;

namespace Training.Dtos;

public partial class AccessTokenDto
{
    [Required]
    public string AccessToken { get; set; }
}