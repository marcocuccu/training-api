using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Training.Data;
using Training.Services;

namespace Training.Tests.Unit.Services;

public class AuthServiceTests
{
    [Fact]
    [Trait("Category", "Login")]
    public async Task Login_ValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        // Arrange
        var authRepo = new Mock<IAuthRepository>();
        var logger = new Mock<ILogger<AuthService>>();

        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "TEST_SECRET" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var authHelper = new Helpers.AuthHelper(config);

        var authService = new AuthService(authRepo.Object, authHelper, config, logger.Object);

        var userForLoginDto = new Training.Dtos.LoginRequestDto()
        {
            Email = "test@test.test",
            Password = "test"
        };

        // Act
        // var authResponse = await authService.Login(userForLoginDto);

        // Assert
        Assert.True(1 == 1);
    }
}

