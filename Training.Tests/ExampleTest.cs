using Xunit;

namespace Training.Tests;

public class ExampleTest
{
    [Fact(DisplayName = "_______ THIS IS AN EXAMPLE, SET THE TEST NAME HERE")]
    [Trait("Category", "CategoryExample")]
    public void MethodName_StateUnderTest_ExpectedBehavior()
    {
        // Naming pattern:
        // MethodName_StateUnderTest_ExpectedBehavior

        // Arrange
        var input = 2;

        // Act
        var result = input + 2;

        // Assert
        Assert.Equal(4, result);
    }

    [Fact(Skip = "Not implemented yet")]
    [Trait("Category", "CategoryExample")]
    public void Login_ValidCredentials_ReturnsAccessAndRefreshToken()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Example external dependency:
        // var authRepository = new FakeAuthRepository();
        // var passwordService = new FakePasswordService();
        // var jwtService = new FakeJwtService();
        // var service = new AuthService(authRepository, passwordService, jwtService);

        // Act
        // var result = service.Login(request);

        // Assert
        // Assert.NotNull(result);
        // Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        // Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        // Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void Login_InvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        // var service = CreateAuthServiceWithInvalidPassword();

        // Act
        // Action act = () => service.Login(new LoginRequestDto(...));

        // Assert
        // Assert.Throws<UnauthorizedAccessException>(act);
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public async Task Login_InvalidPasswordAsync_ThrowsUnauthorizedException()
    {
        // Arrange
        // var service = CreateAuthServiceWithInvalidPassword();

        // Act
        // Func<Task> act = async () => await service.LoginAsync(new LoginRequestDto(...));

        // Assert
        // await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void Register_EmailAlreadyExists_ThrowsValidationException()
    {
        // Arrange
        // var service = CreateAuthServiceWithExistingEmail();

        // Act
        // Action act = () => service.Register(new RegisterRequestDto(...));

        // Assert
        // var exception = Assert.Throws<ValidationException>(act);
        // Assert.Contains("Email", exception.Message);
    }

    [Theory]
    [Trait("Category", "CategoryExample")]
    [InlineData("test@test.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_DifferentInputs_ReturnsExpectedResult(string? email, bool expected)
    {
        // Arrange

        // Act
        // var result = EmailValidator.IsValid(email);

        // Assert
        // Assert.Equal(expected, result);
    }

    [Theory]
    [Trait("Category", "CategoryExample")]
    [MemberData(nameof(InvalidRegisterRequests))]
    public void Register_InvalidInput_ThrowsValidationException(RegisterRequestDto request)
    {
        // Arrange
        // var service = CreateAuthService();

        // Act
        // Action act = () => service.Register(request);

        // Assert
        // Assert.Throws<ValidationException>(act);
    }

    public static IEnumerable<object[]> InvalidRegisterRequests()
    {
        yield return new object[]
        {
            new RegisterRequestDto { Email = "", Password = "Password123!" }
        };

        yield return new object[]
        {
            new RegisterRequestDto { Email = "test@test.com", Password = "" }
        };
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void Refresh_ValidRefreshToken_RotatesToken()
    {
        // Arrange
        // var oldRefreshToken = "raw-refresh-token";

        // Act
        // var result = service.Refresh(oldRefreshToken);

        // Assert
        // Assert.NotNull(result);
        // Assert.NotEqual(oldRefreshToken, result.RefreshToken);
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void Logout_ValidRefreshToken_RevokesToken()
    {
        // Arrange
        // var refreshToken = "raw-refresh-token";

        // Act
        // service.Logout(refreshToken);

        // Assert
        // Assert.True(repository.WasTokenRevoked(refreshToken));
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void PasswordHash_SamePasswordDifferentSalt_ProducesDifferentHashes()
    {
        // Arrange
        var password = "Password123!";

        // Act
        // var hash1 = passwordService.Hash(password, salt1);
        // var hash2 = passwordService.Hash(password, salt2);

        // Assert
        // Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void CreatedUser_ShouldMatchExpectedObject()
    {
        // Arrange
        // var expected = new UserReadDto
        // {
        //     UserId = 1,
        //     FirstName = "Marco",
        //     Email = "test@test.com",
        //     Active = true
        // };

        // Act
        // var actual = service.GetUser(1);

        // Assert
        // Assert.Equal(expected.UserId, actual.UserId);
        // Assert.Equal(expected.Email, actual.Email);
        // Assert.True(actual.Active);
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void TokenExpiry_ShouldBeInTheFuture()
    {
        // Arrange

        // Act
        // var token = jwtService.GenerateAccessToken(userId: 1);

        // Assert
        // Assert.True(token.ExpiresAt > DateTime.UtcNow);
        // Assert.True(token.ExpiresAt <= DateTime.UtcNow.AddMinutes(20));
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void UpdateUser_ValidInput_CallsRepositoryOnce()
    {
        // MOCKING

        // Arrange
        // var repository = new Mock<IUserRepository>();
        // var service = new UserService(repository.Object);

        // Act
        // service.UpdateUser(1, new UserUpdateDto(...));

        // Assert
        // repository.Verify(r => r.UpdateUser(1, It.IsAny<UserUpdateDto>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void Register_WhenRepositoryFails_DoesNotSwallowException()
    {
        // Arrange
        // repository.Setup(r => r.CreateUser(...))
        //           .Throws(new SqlException(...));

        // Act
        // Action act = () => service.Register(request);

        // Assert
        // Assert.Throws<DatabaseException>(act);
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public void Register_WhenCreatingAuthAndUser_UsesTransaction()
    {
        // THIS SHOULD BE AN INTEGRATION TEST

        // Arrange

        // Act
        // service.Register(request);

        // Assert
        // Verify both operations are committed together
        // If one fails, both should be rolled back
    }

    [Fact]
    [Trait("Category", "CategoryExample")]
    public async Task AsyncMethod_ShouldBeAwaitedCorrectly()
    {
        // Arrange

        // Act
        // var result = await service.GetUserAsync(1);

        // Assert
        // Assert.NotNull(result);
    }
}

// Example DTOs only to make the sample easier to read.
// In a real project, these would come from the main API project.
public class LoginRequestDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequestDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

// Example custom exception.
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}

// Example custom exception.
public class DatabaseException : Exception
{
    public DatabaseException(string message) : base(message)
    {
    }
}