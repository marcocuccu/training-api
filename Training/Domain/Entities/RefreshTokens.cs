namespace Training.Entities;

public partial class RefreshTokens
{
    public int RefreshTokenId { set; get; }
    public int UserId { set; get; }
    public string TokenHash { set; get; }

    public DateTime ExpiresAt { set; get; }

    public DateTime? RevokedAt { set; get; }

    public DateTime CreatedAt { set; get; }

    public RefreshTokens()
    {
        TokenHash ??= "";
    }

}