using System.Security.Cryptography;
using Training.Configuration;
using Training.Data;
using Training.Dtos;
using Training.Entities;
using Training.Extensions;
using Training.Helpers;

namespace Training.Services;

public class AuthService
{
    private readonly IAuthRepository _authRepo;
    private readonly AuthHelper _authHelper;
    private readonly JwtSettings _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthRepository authRepo, 
        AuthHelper authHelper, 
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _authRepo = authRepo;
        _authHelper = authHelper;
        _jwt = InitJwt(config);
        _logger = logger;
    }

    private JwtSettings InitJwt(IConfiguration config)
    {
        var jwt = config.GetSection("Jwt").Get<JwtSettings>();
        if (jwt == null)
            throw new Exception("Section Jwt missing in settings");

        return jwt;
    }

    public async Task Register(RegisterRequestDto registerRequestDto)
    {
        // check email is not registered yet
        var emailAlreadyRegistered = await _authRepo.EmailAlreadyRegistered(registerRequestDto.Email);
        if(emailAlreadyRegistered)
            throw new ConflictException("Email already registered");

        // create Salt
        var passwordSalt = _authHelper.CreatePasswordSalt();

        // Hash = func(password + salt + pepper)
        byte[] hash = _authHelper.GetHash(registerRequestDto.Password, passwordSalt);

        // Register user and passwords on DB
        User user = new()
        {
            FirstName = registerRequestDto.FirstName,
            LastName = registerRequestDto.LastName,
            Email = registerRequestDto.Email,
            Gender = registerRequestDto.Gender,
            Active = true
        };
        
        int userId = await _authRepo.RegisterUserAndPassword(passwordSalt, hash, user);

        if(userId <= 0)
            throw new Exception("Failed to register");

        _logger.LogInformation("User {UserId} registered successfully", userId);
    }

    public async Task<AuthResponseDto> Login(LoginRequestDto userForLoginDto)
    {
        // Remove old refresh token
        var deletedTokens = await _authRepo.CleanExpiredTokens();   // Stored procedure
        _logger.LogInformation("Tokens cleanup executed, removed {Count} records", deletedTokens);

        // Grab hash and salt
        AuthCredentials? userForConfirmation = await _authRepo.GetAuthCredentialsByEmail(userForLoginDto.Email);
        if (userForConfirmation == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        // create hash = func(password + salt + pepper)
        byte[] passwordHash = _authHelper.GetHash(userForLoginDto.Password, userForConfirmation.PasswordSalt);

        // chech newHash == HashFromDB
        bool hasMatch = CryptographicOperations.FixedTimeEquals(passwordHash, userForConfirmation.PasswordHash);
        if (!hasMatch)
            throw new UnauthorizedAccessException("Invalid email or password");

        // Return token(userId)
        var accessToken = _authHelper.CreateAccessToken(userForConfirmation.UserId, _jwt.Key, _jwt.Issuer, _jwt.Audience, _jwt.AccessTokenExpiresMin);
        var refreshToken = _authHelper.GenerateRefreshToken();

        await SaveRefreshTokenOnDB(userForConfirmation.UserId, refreshToken);

        _logger.LogInformation("User {UserId} logged in successfully", userForConfirmation.UserId);
        
        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task Logout(RefreshTokenDto refreshToken)
    {
        // Get hash refresh token
        var hashRefreshToken = _authHelper.RefreshTokenToHash(refreshToken.RefreshToken);
        
        // Revoke refresh token in DB
        bool success = await _authRepo.RevokeTokenRefresh(hashRefreshToken);
        if(!success)
        {
            _logger.LogWarning("Logout requested with invalid refresh token");
            // throw new UnauthorizedAccessException("Invalid refresh token");  Not necessary an exception to handle
            return;
        }

        _logger.LogInformation("Logout was successfull, refresh token has been revoked");
    }

    private async Task SaveRefreshTokenOnDB(int userId, string refreshTokenString)
    {   
        // Get hash refresh token
        var hashRefreshToken = _authHelper.RefreshTokenToHash(refreshTokenString);

        // Save on DB
        RefreshTokens refreshToken = new()
        {
            UserId = userId,
            TokenHash = hashRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwt.RefreshTokenExpiresMin),
            CreatedAt = DateTime.UtcNow
        };

        var success = await _authRepo.SaveRefreshToken(refreshToken);
        if(!success)
            throw new Exception("Unable to save refresh token");
    }

    public async Task<AccessTokenDto> RefreshToken(RefreshTokenDto refreshTokenDto)
    {
        // Validate token
        var refreshTokenHash = _authHelper.RefreshTokenToHash(refreshTokenDto.RefreshToken);
        var refreshTokenModel = await _authRepo.GetRefreshTokenModel(refreshTokenHash);
        
        if(refreshTokenModel is null)
        {
            _logger.LogWarning("Refresh token failed, token not found");
            throw new UnauthorizedAccessException("Invalid refresh token");
        }
        if(refreshTokenModel.RevokedAt is not null)
        {
            _logger.LogWarning("Refresh token failed, token revoked");
            throw new UnauthorizedAccessException("Invalid refresh token");
        }
        if(refreshTokenModel.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token failed, token expired");
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Return new access token
        var accessToken = _authHelper.CreateAccessToken(refreshTokenModel.UserId, _jwt.Key, _jwt.Issuer, _jwt.Audience, _jwt.AccessTokenExpiresMin);
        _logger.LogInformation("Access token refreshed for user {UserId}", refreshTokenModel.UserId);
        return new AccessTokenDto
        {
            AccessToken = accessToken
        };
    }
}