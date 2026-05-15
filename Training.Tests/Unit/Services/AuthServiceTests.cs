using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Training.Data;
using Training.Dtos;
using Training.Entities;
using Training.Extensions;
using Training.Helpers;
using Training.Services;

namespace Training.Tests.Unit.Services;

public class AuthServiceTests
{
    #region FACTORY
    private IConfiguration CreateConfig()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "THIS_IS_A_LONG_TEST_SECRET_KEY_FOR_HMAC_SHA512_SIGNATURE_1234567890" },
            { "Jwt:Issuer", "Training.Tests" },
            { "Jwt:Audience", "Training.Tests.Client" },
            { "Jwt:AccessTokenExpiresMin", "15" },
            { "Jwt:RefreshTokenExpiresMin", "10080" },

            { "AppSettings:PasswordPepper", "TEST_PASSWORD_PEPPER_1234567890" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private AuthCredentials CreateCredentials(AuthHelper authHelper, int userId = 1, string password = "Password123!")
    {
        byte[] salt = authHelper.CreatePasswordSalt();
        byte[] hash = authHelper.GetHash(password, salt);

        return new AuthCredentials()
        {
            AuthId = 1,
            UserId = userId,
            PasswordSalt = salt,
            PasswordHash = hash
        };
    }

    private AuthService CreateAuthService(
        Mock<IAuthRepository> authRepo,
        AuthHelper authHelper,
        IConfiguration config)
    {
        var logger = new Mock<ILogger<AuthService>>();

        return new AuthService(authRepo.Object, authHelper, config, logger.Object);
    }
    
    private RegisterRequestDto CreateRegisterRequest(string email, string password)
    {
        return new RegisterRequestDto()
        {
            FirstName = "FirstName",
            LastName = "LastName",
            Email = email,
            Gender = "Gender",
            Password = password,
            PasswordConfirmation = password
        };
    }
    #endregion

    [Fact]
    [Trait("Category", "Login")]
    public async Task Login_WhenCredentialsAreValid_ReturnsAccessAndRefreshTokens()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new Helpers.AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        var loginRequest = new LoginRequestDto()
        {
            Email = "test@email.com",
            Password = "Password123!"
        };

        var credentials = CreateCredentials(authHelper, userId: 42, password: loginRequest.Password);
        
        authRepo
            .Setup(repo => repo.CleanExpiredTokens())
            .ReturnsAsync(0);

        authRepo
            .Setup(repo => repo.GetAuthCredentialsByEmail(loginRequest.Email))
            .ReturnsAsync(credentials);

        authRepo
            .Setup(repo => repo.SaveRefreshToken(It.IsAny<RefreshTokens>()))
            .ReturnsAsync(true);

        var authService = CreateAuthService(authRepo, authHelper, config);

        // Act
        var result = await authService.Login(loginRequest);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));

        authRepo.Verify(repo => repo.CleanExpiredTokens(), Times.Once);
        authRepo.Verify(repo => repo.GetAuthCredentialsByEmail(loginRequest.Email), Times.Once);
        authRepo.Verify(repo => repo.SaveRefreshToken(
            It.Is<RefreshTokens>(t => 
                t.UserId == 42 &&
                !string.IsNullOrWhiteSpace(t.TokenHash) &&
                t.TokenHash != result.RefreshToken &&
                t.ExpiresAt > DateTime.UtcNow &&
                t.CreatedAt <= DateTime.UtcNow)),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Login")]
    public async Task Login_WhenEmailDoesNotExist_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        LoginRequestDto loginRequest = new LoginRequestDto()
        {
            Email = "missing@email.com",
            Password = "Password123!"
        };
        
        authRepo
            .Setup(repo => repo.CleanExpiredTokens())
            .ReturnsAsync(0);

        authRepo
            .Setup(repo => repo.GetAuthCredentialsByEmail(loginRequest.Email))
            .ReturnsAsync((AuthCredentials?)null);

        
        var authService = CreateAuthService(authRepo, authHelper, config);

        // Act
        async Task Act() => await authService.Login(loginRequest);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(Act);
        authRepo.Verify(repo => repo.SaveRefreshToken(It.IsAny<RefreshTokens>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Login")]
    public async Task Login_WhenPasswordIsNotValid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        LoginRequestDto loginRequest = new LoginRequestDto()
        {
            Email = "email@email.com",
            Password = "invalid123!"
        };
        
        var credentials = CreateCredentials(authHelper, userId: 42, password: "ValidPassword123!");

        authRepo
            .Setup(repo => repo.CleanExpiredTokens())
            .ReturnsAsync(0);

        authRepo
            .Setup(repo => repo.GetAuthCredentialsByEmail(loginRequest.Email))
            .ReturnsAsync(credentials);

        var authService = CreateAuthService(authRepo, authHelper, config);

        // Act
        async Task Act() => await authService.Login(loginRequest);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(Act);
        authRepo.Verify(repo => repo.SaveRefreshToken(It.IsAny<RefreshTokens>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Login")]
    public async Task Login_WhenRefreshTokenCannotBeSaved_ThrowsException()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        LoginRequestDto loginRequest = new LoginRequestDto()
        {
            Email = "email@email.com",
            Password = "Password123!"
        };

        var credentials = CreateCredentials(authHelper, userId: 42, password: loginRequest.Password);
        
        authRepo
            .Setup(repo => repo.CleanExpiredTokens())
            .ReturnsAsync(0);

        authRepo
            .Setup(repo => repo.GetAuthCredentialsByEmail(loginRequest.Email))
            .ReturnsAsync(credentials);

        authRepo
            .Setup(repo => repo.SaveRefreshToken(It.IsAny<RefreshTokens>()))
            .ReturnsAsync(false);

        
        var authService = CreateAuthService(authRepo, authHelper, config);

        // Act
        async Task Act() => await authService.Login(loginRequest);

        // Assert
        await Assert.ThrowsAsync<Exception>(Act);
    }

    [Fact]
    [Trait("Category", "Register")]
    public async Task Register_WhenRegistrationSuccessful_CreateUserAndPassword()
    {
        // Arrange
        int userId = 42;
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        var registerRequestDto = CreateRegisterRequest("email@email.com", "Password123!");

        authRepo
            .Setup(repo => repo.EmailAlreadyRegistered(registerRequestDto.Email))
            .ReturnsAsync(false);

        authRepo
            .Setup(repo => repo.RegisterUserAndPassword(
                It.IsAny<byte[]>(), 
                It.IsAny<byte[]>(), 
                It.IsAny<User>()))
            .ReturnsAsync(userId);

        var authService = CreateAuthService(authRepo, authHelper, config);

        // Act
        await authService.Register(registerRequestDto);

        // Assert
        authRepo.Verify(repo => repo.EmailAlreadyRegistered(It.Is<string>(s => s == registerRequestDto.Email)), Times.Once);
        authRepo.Verify(repo => repo.RegisterUserAndPassword(
                It.Is<byte[]>(salt => salt.Length == 16), 
                It.Is<byte[]>(hash => hash.Length == 32), 
                It.Is<User>(u => 
                    u.FirstName == registerRequestDto.FirstName &&
                    u.LastName == registerRequestDto.LastName &&
                    u.Email == registerRequestDto.Email &&
                    u.Gender == registerRequestDto.Gender &&
                    u.Active == true)), 
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Register")]
    public async Task Register_WhenEmailAlreadyRegistered_ThrowsConflictException()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        var registerRequestDto = CreateRegisterRequest("invalid@email.com", "Password123!");

        authRepo
            .Setup(repo => repo.EmailAlreadyRegistered(registerRequestDto.Email))
            .ReturnsAsync(true);

        var authService = CreateAuthService(authRepo, authHelper, config);

        // Act
        async Task Act() => await authService.Register(registerRequestDto);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(Act);
        authRepo.Verify(repo => repo.EmailAlreadyRegistered(registerRequestDto.Email), Times.Once);
        authRepo.Verify(repo => repo.RegisterUserAndPassword(
                It.IsAny<byte[]>(), 
                It.IsAny<byte[]>(), 
                It.IsAny<User>()), 
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Register")]
    public async Task Register_WhenRepoReturnsInvalidUserId_ThrowsException()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        var registerRequestDto = CreateRegisterRequest("email@email.com", "Password123!");

        authRepo
            .Setup(repo => repo.EmailAlreadyRegistered(registerRequestDto.Email))
            .ReturnsAsync(false);

        authRepo
            .Setup(repo => repo.RegisterUserAndPassword(
                It.IsAny<byte[]>(), 
                It.IsAny<byte[]>(), 
                It.IsAny<User>()))
            .ReturnsAsync(0);

        var authService = CreateAuthService(authRepo, authHelper, config);

        // Act
        async Task Act() => await authService.Register(registerRequestDto);

        // Assert
        await Assert.ThrowsAsync<Exception>(Act);

        authRepo.Verify(repo => repo.EmailAlreadyRegistered(registerRequestDto.Email), Times.Once);
        authRepo.Verify(repo => repo.RegisterUserAndPassword(
                It.IsAny<byte[]>(), 
                It.IsAny<byte[]>(), 
                It.IsAny<User>()), 
            Times.Once);
    }

    [Fact]
    [Trait("Category", "RefreshToken")]
    public async Task RefreshToken_WhenValidRefreshToken_ReturnsAccessToken()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        string refreshToken = "Refresh_Token_Long_String_1234567890";
        string refreshTokenHash = authHelper.RefreshTokenToHash(refreshToken);

        var refreshTokenDto = new RefreshTokenDto()
        {
            RefreshToken = refreshToken
        };

        var refreshTokenModel = new RefreshTokens
        {
            RefreshTokenId = 1,
            UserId = 42,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            RevokedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        authRepo
            .Setup(repo => repo.GetRefreshTokenModel(refreshTokenHash))
            .ReturnsAsync(refreshTokenModel);

        var authService = CreateAuthService(authRepo, authHelper, config);
        
        
        // Act
        var accessToken = await authService.RefreshToken(refreshTokenDto);

        // Assert
        Assert.NotNull(accessToken);
        Assert.False(string.IsNullOrWhiteSpace(accessToken.AccessToken));
        authRepo.Verify(repo => repo.GetRefreshTokenModel(refreshTokenHash), Times.Once);
    }

    [Fact]
    [Trait("Category", "RefreshToken")]
    public async Task RefreshToken_WhenTokenNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        string refreshToken = "Refresh_Token_Long_String_1234567890";
        string refreshTokenHash = authHelper.RefreshTokenToHash(refreshToken);

        var refreshTokenDto = new RefreshTokenDto()
        {
            RefreshToken = refreshToken
        };

        authRepo
            .Setup(repo => repo.GetRefreshTokenModel(refreshTokenHash))
            .ReturnsAsync((RefreshTokens?)null);

        var authService = CreateAuthService(authRepo, authHelper, config);
        
        
        // Act
        async Task Act() => await authService.RefreshToken(refreshTokenDto);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(Act);
        authRepo.Verify(repo => repo.GetRefreshTokenModel(refreshTokenHash), Times.Once);
    }

    [Fact]
    [Trait("Category", "RefreshToken")]
    public async Task RefreshToken_WhenTokenRevoked_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        string refreshToken = "Refresh_Token_Long_String_1234567890";
        string refreshTokenHash = authHelper.RefreshTokenToHash(refreshToken);

        var refreshTokenDto = new RefreshTokenDto()
        {
            RefreshToken = refreshToken
        };

        var refreshTokenModel = new RefreshTokens
        {
            RefreshTokenId = 1,
            UserId = 42,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            RevokedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        authRepo
            .Setup(repo => repo.GetRefreshTokenModel(refreshTokenHash))
            .ReturnsAsync(refreshTokenModel);

        var authService = CreateAuthService(authRepo, authHelper, config);
        
        // Act
        async Task Act() => await authService.RefreshToken(refreshTokenDto);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(Act);
        authRepo.Verify(repo => repo.GetRefreshTokenModel(refreshTokenHash), Times.Once);
    }

    [Fact]
    [Trait("Category", "RefreshToken")]
    public async Task RefreshToken_WhenTokenExpired_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        string refreshToken = "Refresh_Token_Long_String_1234567890";
        string refreshTokenHash = authHelper.RefreshTokenToHash(refreshToken);

        var refreshTokenDto = new RefreshTokenDto()
        {
            RefreshToken = refreshToken
        };

        var refreshTokenModel = new RefreshTokens
        {
            RefreshTokenId = 1,
            UserId = 42,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RevokedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        authRepo
            .Setup(repo => repo.GetRefreshTokenModel(refreshTokenHash))
            .ReturnsAsync(refreshTokenModel);

        var authService = CreateAuthService(authRepo, authHelper, config);
        
        // Act
        async Task Act() => await authService.RefreshToken(refreshTokenDto);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(Act);
        authRepo.Verify(repo => repo.GetRefreshTokenModel(refreshTokenHash), Times.Once);
    }

    [Fact]
    [Trait("Category", "Logout")]
    public async Task Logout_WhenRefreshTokenExists_RevokesRefreshToken()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        string refreshToken = "Refresh_Token_Long_String_1234567890";
        string refreshTokenHash = authHelper.RefreshTokenToHash(refreshToken);

        var refreshTokenDto = new RefreshTokenDto()
        {
            RefreshToken = refreshToken
        };

        authRepo
            .Setup(repo => repo.RevokeTokenRefresh(refreshTokenHash))
            .ReturnsAsync(true);

        var authService = CreateAuthService(authRepo, authHelper, config);

        // Act
        await authService.Logout(refreshTokenDto);

        // Assert
        authRepo.Verify(repo => repo.RevokeTokenRefresh(refreshTokenHash), Times.Once);
    }

    [Fact]
    [Trait("Category", "Logout")]
    public async Task Logout_WhenRefreshTokenDoesNotExist_DoesNotThrowException()
    {
        // Arrange
        var config = CreateConfig();
        var authHelper = new AuthHelper(config);
        var authRepo = new Mock<IAuthRepository>();

        string refreshToken = "Refresh_Token_Long_String_1234567890";
        string refreshTokenHash = authHelper.RefreshTokenToHash(refreshToken);

        var refreshTokenDto = new RefreshTokenDto()
        {
            RefreshToken = refreshToken
        };

        authRepo
            .Setup(repo => repo.RevokeTokenRefresh(refreshTokenHash))
            .ReturnsAsync(false);

        var authService = CreateAuthService(authRepo, authHelper, config);

        // Act
        Exception? exception = await Record.ExceptionAsync(() => authService.Logout(refreshTokenDto));

        // Assert
        Assert.Null(exception);
        authRepo.Verify(repo => repo.RevokeTokenRefresh(refreshTokenHash), Times.Once);
    }
}

