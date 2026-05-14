using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;

namespace Training.Helpers;

public class AuthHelper
{
    private readonly IConfiguration _config;
    
    public AuthHelper(IConfiguration config)
    {
        _config = config;
    }

    public byte[] GetHash(string userPassword, byte[] passwordSalt)
    {
        string passwordPepperString = _config.GetSection("AppSettings:PasswordPepper").Value ??= "";
        if (string.IsNullOrEmpty(passwordPepperString))
            throw new Exception("AppSettings:PasswordPepper missing");
        byte[] passwordPepperByte = Encoding.UTF8.GetBytes(passwordPepperString);

        byte[] passwordSaltPepper = new byte[passwordSalt.Length + passwordPepperByte.Length];

        Buffer.BlockCopy(passwordSalt, 0, passwordSaltPepper, 0, passwordSalt.Length);
        Buffer.BlockCopy(passwordPepperByte, 0, passwordSaltPepper, passwordSalt.Length, passwordPepperByte.Length);

        return KeyDerivation.Pbkdf2(
            password: userPassword,
            salt: passwordSaltPepper,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8
        );
    }

    public byte[] GetHash(string userPassword, string passwordSalt)
    {
        byte[] passwordSaltByte = Encoding.UTF8.GetBytes(passwordSalt);
        return GetHash(userPassword, passwordSaltByte);
    }

    public string CreateAccessToken(int userId, string key, string issuer, string audience, double accessTokenExpiresMin)
    {
        // Prepare UserId
        Claim[] claims = [ new Claim(ClaimTypes.NameIdentifier, userId.ToString()) ];

        // Prepare token key
        SymmetricSecurityKey tokenKey = new(Encoding.UTF8.GetBytes(key));
        SigningCredentials credentials = new(tokenKey, SecurityAlgorithms.HmacSha512Signature);

        // Create a descriptor
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddMinutes(accessTokenExpiresMin)
        };

        // Create the token with the descriptor
        JwtSecurityTokenHandler tokenHandler = new();
        SecurityToken token = tokenHandler.CreateToken(descriptor);

        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Generate a random string
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public string RefreshTokenToHash(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hash);
    }


    public byte[] CreatePasswordSalt()
    {
        byte[] passwordSalt = new byte[128/8];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetNonZeroBytes(passwordSalt);
        }

        return passwordSalt;
    }
}