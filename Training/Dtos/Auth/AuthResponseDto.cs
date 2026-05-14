namespace Training.Dtos;

public class AuthResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

    public AuthResponseDto()
    {
        AccessToken ??= "";
        RefreshToken ??= "";
    }
}