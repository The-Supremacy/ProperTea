using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ProperTea.Identity.Api.DTOs;
using ProperTea.Identity.IntegrationTests.Api.Setup;
using ProperTea.Identity.IntegrationTests.Setup;
using ProperTea.Identity.Kernel.Models;
using Shouldly;

namespace ProperTea.Identity.IntegrationTests.Api;

public class AuthEndpointsTests : IClassFixture<IdentityServiceFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityServiceFactory _factory;

    public AuthEndpointsTests(IdentityServiceFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task ChangePassword_WithInvalidOldPassword_ReturnsBadRequest()
    {
        // Arrange
        const string email = "invalid.change.password.user@example.com";
        const string oldPassword = "ValidPassword123!";
        const string newPassword = "NewValidPassword123!";

        var registrationRequest = new RegisterRequest(email, oldPassword);
        await _client.PostAsJsonAsync("/api/auth/register", registrationRequest);

        var loginRequest = new LoginRequest(email, oldPassword);
        var loginResponse = await _client.PostAsJsonAsync("/api/token/login", loginRequest);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var changePasswordRequest = new ChangePasswordRequest("WrongOldPassword!", newPassword);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(changePasswordRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        const string email = "invalid.change.password.user@example.com";
        const string oldPassword = "ValidPassword123!";
        const string newPassword = "NewValidPassword123!";

        var registrationRequest = new RegisterRequest(email, oldPassword);
        await _client.PostAsJsonAsync("/api/auth/register", registrationRequest);

        var loginRequest = new LoginRequest(email, oldPassword);
        var loginResponse = await _client.PostAsJsonAsync("/api/token/login", loginRequest);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var changePasswordRequest = new ChangePasswordRequest(oldPassword, newPassword);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(changePasswordRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", authResponse!.AccessToken + "tampered");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithValidOldPassword_ReturnsOk()
    {
        // Arrange
        const string email = "change.password.user@example.com";
        const string oldPassword = "ValidPassword123!";
        const string newPassword = "NewValidPassword123!";

        var registrationRequest = new RegisterRequest(email, oldPassword);
        await _client.PostAsJsonAsync("/api/auth/register", registrationRequest);

        var loginRequest = new LoginRequest(email, oldPassword);
        var loginResponse = await _client.PostAsJsonAsync("/api/token/login", loginRequest);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var changePasswordRequest = new ChangePasswordRequest(oldPassword, newPassword);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
        {
            Content = JsonContent.Create(changePasswordRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify password was changed
        var newLoginRequest = new LoginRequest(email, newPassword);
        var newLoginResponse = await _client.PostAsJsonAsync("/api/token/login", newLoginRequest);
        newLoginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithExistingUser_ReturnsOk()
    {
        // Arrange
        const string email = "forgot.password.user@example.com";

        var registrationRequest = new RegisterRequest(email, "ValidPassword123!");
        await _client.PostAsJsonAsync("/api/auth/register", registrationRequest);
        var forgotPasswordRequest = new ForgotPasswordRequest(email);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotPasswordRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentUser_ReturnsOk()
    {
        // Arrange
        var forgotPasswordRequest = new ForgotPasswordRequest("non.existent.user@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotPasswordRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }


    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest("existing.user@example.com", "ValidPassword123!");
        await _client.PostAsJsonAsync("/api/auth/register", request); // First registration

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request); // Second attempt

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedAndToken()
    {
        // Arrange
        const string email = "test.user@example.com";

        var request = new RegisterRequest(email, "ValidPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

        authResponse.ShouldNotBeNull();
        authResponse.Email.ShouldBe(email);
        authResponse.AccessToken.ShouldNotBeNullOrEmpty();
        authResponse.UserId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        const string email = "invalid.token.user@example.com";
        const string password = "ValidPassword123!";
        const string newPassword = "NewValidPassword123!";

        var registrationRequest = new RegisterRequest(email, password);
        await _client.PostAsJsonAsync("/api/auth/register", registrationRequest);

        var resetPasswordRequest = new ResetPasswordRequest(email, newPassword, "invalidtoken");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", resetPasswordRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ReturnsOk()
    {
        // Arrange
        var email = "reset.password.user@example.com";
        var oldPassword = "ValidPassword123!";
        var newPassword = "NewValidPassword123!";

        // We have to do it through service directly,
        // as in future the reset token is to be send to email and not in the response
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ProperTeaUser>>();

        var user = new ProperTeaUser { UserName = email, Email = email, EmailConfirmed = true };
        await userManager.CreateAsync(user, oldPassword);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetPasswordRequest = new ResetPasswordRequest(email, token, newPassword);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", resetPasswordRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify password was changed
        var loginRequest = new LoginRequest(email, newPassword);
        var loginResponse = await _client.PostAsJsonAsync("/api/token/login", loginRequest);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}