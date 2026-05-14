using Training.Entities;

namespace Training.Data;

public interface IAuthRepository
{
    public Task<AuthCredentials?> GetAuthCredentialsByEmail(string email);
    public Task<bool> EmailAlreadyRegistered(string email);
    public Task<bool> SaveRefreshToken(RefreshTokens refreshToken);
    public Task<RefreshTokens?> GetRefreshTokenModel(string tokenHash);
    public Task<int> RegisterUserAndPassword(byte[] passwordSalt, byte[] passwordHash, User user);
    public Task<bool> RevokeTokenRefresh(string tokenHash);
    public Task<int> CleanExpiredTokens();
}