using System.Net;
using System.Net.Http.Json;
using ProperTea.Identity.Service.DTOs;
using ProperTea.Identity.Tests.Utility;
using Shouldly;

namespace ProperTea.Identity.Tests;

public class TokenEndpointsTests : IClassFixture<IdentityServiceFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityServiceFactory _factory;

    public TokenEndpointsTests(IdentityServiceFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }
    
    [Fact]
    public async Task ExternalCallback_WithNonExistingUser_ReturnsUnauthorizedChallenge()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/api/token/external/callback");
        var response2 = await _client.GetAsync("/api/token/external/callback");

        // Assert
        // Check first response - must be success.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.ShouldNotBeNull();
        authResponse.Email.ShouldBe(TestExternalSchemeHandler.TestUserId);
        authResponse.AccessToken.ShouldNotBeNullOrEmpty();

        // Check second response - must be success with an existing user.
        response2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var authResponse2 = await response2.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse2.ShouldNotBeNull();
        authResponse2.Email.ShouldBe(TestExternalSchemeHandler.TestUserId);
        authResponse2.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExternalCallback_WithNewUser_CreatesUserAndReturnsToken()
    {
        // Arrange
        const string provider = TestExternalSchemeHandler.DefaultScheme;

        // Act
        var response = await _client.GetAsync($"/api/token/external/{provider}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        const string email = "invalid.password@example.com";

        var registrationRequest = new RegisterRequest(email, "ValidPassword123!");
        await _client.PostAsJsonAsync("/api/auth/register", registrationRequest);
        var loginRequest = new LoginRequest(email, "WrongPassword!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/token/login", loginRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("non.existent.user@example.com", "SomePassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/token/login", loginRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        // Arrange
        const string email = "login.user@example.com";
        const string password = "ValidPassword123!";

        var registrationRequest = new RegisterRequest(email, password);
        await _client.PostAsJsonAsync("/api/auth/register", registrationRequest);
        var loginRequest = new LoginRequest(email, password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/token/login", loginRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.ShouldNotBeNull();
        authResponse.Email.ShouldBe(email);
        authResponse.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Reissue_WithValidRefreshToken_ReturnsNewToken()
    {
        // Arrange
        const string email = "reissue.user@example.com";
        const string password = "ValidPassword123!";

        var registrationRequest = new RegisterRequest(email, password);
        await _client.PostAsJsonAsync("/api/auth/register", registrationRequest);
        var loginRequest = new LoginRequest(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/token/login", loginRequest);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var reissueRequest = new ReissueRequest(authResponse!.AccessToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/token/reissue", reissueRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newAuthResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuthResponse.ShouldNotBeNull();
        newAuthResponse.AccessToken.ShouldNotBeNullOrEmpty();
        newAuthResponse.AccessToken.ShouldNotBe(authResponse.AccessToken);
    }

    [Fact]
    public async Task Reissue_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        const string email = "invalid.reissue.user@example.com";
        const string password = "ValidPassword123!";

        var registrationRequest = new RegisterRequest(email, password);
        await _client.PostAsJsonAsync("/api/auth/register", registrationRequest);
        var loginRequest = new LoginRequest(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/token/login", loginRequest);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var reissueRequest = new ReissueRequest(authResponse!.AccessToken + "tampered");

        // Act
        var response = await _client.PostAsJsonAsync("/api/token/reissue", reissueRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}