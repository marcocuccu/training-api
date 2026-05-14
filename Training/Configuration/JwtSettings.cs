namespace Training.Configuration;

public class JwtSettings
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int AccessTokenExpiresMin { get; set; }
    public int RefreshTokenExpiresMin { get; set; }

    public JwtSettings()
    {
        Key ??= "";
        Issuer ??= "";
        Audience ??= "";
    }
}