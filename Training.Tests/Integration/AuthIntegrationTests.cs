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
        var registerRequest = CreateRegisterRequest(email, password);

        // Act
        var registerResponse = await client.PostAsJsonAsync("/Auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // LOGIN
        // Arrange
        var loginRequest = CreateLoginRequest(email, password);

        // Act
        var loginResponse = await client.PostAsJsonAsync("/Auth/Login", loginRequest);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(authResponse.RefreshToken));

        // GET USERS
        // Arrange

        // Act
        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/User");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
        var getResponse = await client.SendAsync(getRequest);

        var body = await getResponse.Content.ReadAsStringAsync();
        var wwwAuthenticate = getResponse.Headers.WwwAuthenticate.ToString();

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var users = await getResponse.Content.ReadFromJsonAsync<List<UserReadDto>>();

        Assert.NotNull(users);
        Assert.Contains(users, u => u.Email == email);
    }
}