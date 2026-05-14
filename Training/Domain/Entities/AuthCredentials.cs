namespace Training.Entities;

public partial class AuthCredentials
{
    public int AuthId  { get; set; }
    public int UserId  { get; set; }
    public byte[] PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }

    public AuthCredentials()
    {
        PasswordHash ??= [];
        PasswordSalt ??= [];
    }
}