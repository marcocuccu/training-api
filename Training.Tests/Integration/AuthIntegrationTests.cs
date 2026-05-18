using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Training.Dtos;

namespace Training.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthIntegrationTests(IntegrationTestFixture fixture)
    {
        _factory = fixture.Factory;
    }

    #region HELPERS
    private static async Task RegisterUser(HttpClient client, string email, string password)
    {
        // Act
        var response = await client.PostAsJsonAsync("/Auth/register", CreateRegisterRequest(email, password));
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<AuthResponseDto> LoginUser(HttpClient client, string email, string password)
    {
        // Arrange
        var loginRequest = CreateLoginRequest(email, password);

        // Act
        var response = await client.PostAsJsonAsync("/Auth/login", loginRequest);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(authResponse.RefreshToken));

        return authResponse;
    }

    private static RegisterRequestDto CreateRegisterRequest(string email, string password)
    {
        return new RegisterRequestDto
        {
            FirstName = "First",
            LastName = "Last",
            Email = email,
            Gender = "Male",
            Password = password,
            PasswordConfirmation = password
        };
    }

    private static LoginRequestDto CreateLoginRequest(string email, string password)
    {
        return new LoginRequestDto()
        {
            Email = email,
            Password = password
        };
    }

    private static string CreateUniqueEmail()
    {
        return $"{Guid.NewGuid():N}@example.com";
    }
    #endregion

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RegisterLoginGetUsers_WhenFlowCorrect_ReturnsOk()
    {
        // REGISTER
        // Arrange
        string email = CreateUniqueEmail();
        string password = "Password123!";
        var client = _factory.CreateClient();

        // Act&Assert
        await RegisterUser(client, email, password);


        // LOGIN
        // Arrange, Act & Assert
        var authResponse = await LoginUser(client, email, password);


        // GET USERS
        // Arrange
        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/User");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        
        // Act
        var getResponse = await client.SendAsync(getRequest);
        var body = await getResponse.Content.ReadAsStringAsync();
        var wwwAuthenticate = getResponse.Headers.WwwAuthenticate.ToString();

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var users = await getResponse.Content.ReadFromJsonAsync<List<UserReadDto>>();
        Assert.NotNull(users);
        Assert.Contains(users, u => u.Email == email);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_WhenInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerRequest = new RegisterRequestDto()
        {
            FirstName = null,
            LastName = null,
            Email = "notAnEmail",
            Password = "",
            PasswordConfirmation = "1"
        };

        // Act
        var registerResponse = await client.PostAsJsonAsync("/Auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, registerResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
    {
        // ARRANGE
        var client = _factory.CreateClient();
        string email = CreateUniqueEmail();
        string password = "Password123!";
        var registerRequest = CreateRegisterRequest(email, password);

        // ACT
        await RegisterUser(client, email, password);
        var secondRegistrationResponse = await client.PostAsJsonAsync("/Auth/register", registerRequest);
        
        // ASSERT
        Assert.Equal(HttpStatusCode.Conflict, secondRegistrationResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Request_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var getResponse = await client.GetAsync("/User");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_WhenWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = CreateUniqueEmail();
        string correctPassword = "correctPassword";
        string wrongPassword = "wrongPassword";
        var wrongLoginRequest = CreateLoginRequest(email, wrongPassword);

        await RegisterUser(client, email, correctPassword);

        // Act
        var response = await client.PostAsJsonAsync("/Auth/login", wrongLoginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RefreshToken_WhenTokenIsValid_ReturnsNewAccessToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = CreateUniqueEmail();
        string password = "Password123!";

        await RegisterUser(client, email, password);

        var loginResponse = await LoginUser(client, email, password);
        var refreshtoken = new RefreshTokenDto()
        {
            RefreshToken = loginResponse.RefreshToken
        };

        // Act
        var refreshResponse = await client.PostAsJsonAsync("/Auth/refreshtoken", refreshtoken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var accessTokenResponse = await refreshResponse.Content.ReadFromJsonAsync<AccessTokenDto>();
        Assert.NotNull(accessTokenResponse);
        Assert.False(string.IsNullOrWhiteSpace(accessTokenResponse.AccessToken));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Logout_WhenRequestIsValid_RevokesToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = CreateUniqueEmail();
        string password = "Password123!";

        await RegisterUser(client, email, password);
        
        var authResponse = await LoginUser(client, email, password);
        
        var refreshToken = new RefreshTokenDto()
        {
            RefreshToken = authResponse.RefreshToken
        };

        // Act
        var logoutResponse = await client.PostAsJsonAsync("/Auth/logout", refreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // USING SAME REFRESH TOKEN IS NOW REVOKED
        // Act
        var refreshResponse = await client.PostAsJsonAsync("/Auth/refreshtoken", refreshToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}