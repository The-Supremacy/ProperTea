using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ProperTea.Landlord.Bff.Middleware;
using ProperTea.Landlord.Bff.Tests.Utility;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace ProperTea.Landlord.Bff.Tests;

[Collection("Sequential")]
public class AuthEndpointsTests : IClassFixture<LandlordBffServiceFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly LandlordBffServiceFactory _factory;

    public AuthEndpointsTests(LandlordBffServiceFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _factory.IdentityServiceMock.Reset();
    }

    [Fact]
    public async Task Login_WithValidCredentials_CreatesSessionAndSetsCookie()
    {
        // Arrange
        var loginRequest = new { email = "test@example.com", password = "ValidPassword123!" };

        var authResponse = new
        {
            accessToken = "fake-access-token",
            user = new
            {
                id = Guid.NewGuid().ToString(),
                email = "test@example.com"
            }
        };

        // Configure the mock Identity service to return a successful response
        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/login").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(authResponse));

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify that the JWT is NOT in the response body
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldNotContain("fake-access-token");

        // Verify that a session cookie was set
        response.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        var sessionCookie = cookies!.FirstOrDefault(c => c.StartsWith("properteasession"));
        sessionCookie.ShouldNotBeNull();
        sessionCookie.ShouldContain("HttpOnly");
        sessionCookie.ShouldContain("Secure");
    }

    [Fact]
    public async Task Login_WithMalformedIdentityResponse_ReturnsInternalServerError()
    {
        // Arrange
        var loginRequest = new { email = "test@example.com", password = "ValidPassword123!" };

        var malformedAuthResponse = new
        {
            user = new
            {
                id = Guid.NewGuid().ToString(),
                email = "test@example.com"
            }
        };

        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/login").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(malformedAuthResponse));

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Headers.TryGetValues("Set-Cookie", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Logout_WithActiveSession_DeletesSessionCookie()
    {
        // Arrange
        var jwtString = CreateJwt(TimeSpan.FromMinutes(10));

        var loginRequest = new { email = "test@example.com", password = "ValidPassword123!" };
        var authResponse = new
        {
            accessToken = jwtString,
            user = new { id = Guid.NewGuid().ToString() }
        };

        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/login").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(authResponse));

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        var sessionCookie = cookies!.First(c => c.StartsWith(SessionManagementMiddleware.SessionCookieName));

        // Act
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/logout");
        logoutRequest.Headers.Add("Cookie", sessionCookie);
        var logoutResponse = await _client.SendAsync(logoutRequest);

        // Assert
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        logoutResponse.Headers.TryGetValues("Set-Cookie", out var expiredCookies).ShouldBeTrue();
        var expiredCookie =
            expiredCookies!.FirstOrDefault(c => c.StartsWith(SessionManagementMiddleware.SessionCookieName));
        expiredCookie.ShouldNotBeNull();
        expiredCookie.ShouldContain("expires=Thu, 01 Jan 1970"); // The standard way to expire a cookie.
    }

    [Fact]
    public async Task SessionMiddleware_WhenTokenIsExpiring_ReissuesTokenSuccessfully()
    {
        // Arrange
        var expiringJwt = CreateJwt(TimeSpan.FromMinutes(1));
        var reissuedJwt = CreateJwt(TimeSpan.FromMinutes(10));

        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/login").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(new { accessToken = expiringJwt, user = new { id = Guid.NewGuid().ToString() } }));

        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/reissue").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(new { accessToken = reissuedJwt }));

        // Generic protected endpoint. This is just to trigger the middleware.
        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/protected").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = "test@example.com", password = "ValidPassword123!" });
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        var sessionCookie = cookies!.First(c => c.StartsWith(SessionManagementMiddleware.SessionCookieName));

        // Act    
        var protectedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/protected");
        protectedRequest.Headers.Add("Cookie", sessionCookie);
        var protectedResponse = await _client.SendAsync(protectedRequest);

        // Assert
        protectedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var reissueRequests = _factory.IdentityServiceMock.FindLogEntries(
            Request.Create().WithPath("/api/auth/reissue").UsingPost()
        );
        reissueRequests.Count().ShouldBe(1);
    }

    [Fact]
    public async Task SessionMiddleware_WithInvalidSessionId_ClearsCookieAndReturnsUnauthorized()
    {
        // Arrange
        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/protected").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Unauthorized));

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/protected");
        request.Headers.Add("Cookie", $"{SessionManagementMiddleware.SessionCookieName}=invalid-session-id");
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.TryGetValues("Set-Cookie", out var expiredCookies).ShouldBeTrue();
        var expiredCookie =
            expiredCookies!.FirstOrDefault(c => c.StartsWith(SessionManagementMiddleware.SessionCookieName));
        expiredCookie.ShouldNotBeNull();
        expiredCookie.ShouldContain("expires=Thu, 01 Jan 1970");
    }

    [Fact]
    public async Task SessionMiddleware_WhenReissueFails_UsesOldToken()
    {
        // Arrange
        var expiringJwt = CreateJwt(TimeSpan.FromMinutes(1));

        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/login").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(new { accessToken = expiringJwt, user = new { id = Guid.NewGuid().ToString() } }));

        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/reissue").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/protected").UsingGet()
                .WithHeader("Authorization", $"Bearer {expiringJwt}"))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = "test@example.com", password = "ValidPassword123!" });
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        var sessionCookie = cookies!.First(c => c.StartsWith(SessionManagementMiddleware.SessionCookieName));

        // Act       
        var protectedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/protected");
        protectedRequest.Headers.Add("Cookie", sessionCookie);
        var protectedResponse = await _client.SendAsync(protectedRequest);

        // Assert
        protectedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var reissueRequests = _factory.IdentityServiceMock.FindLogEntries(
            Request.Create().WithPath("/api/auth/reissue").UsingPost()
        );
        reissueRequests.Count().ShouldBe(1);
    }

    [Fact]
    public async Task SessionMiddleware_WithValidSession_ForwardsAuthorizationHeader()
    {
        // Arrange
        var jwt = CreateJwt(TimeSpan.FromMinutes(10));

        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/login").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBodyAsJson(new { accessToken = jwt, user = new { id = Guid.NewGuid().ToString() } }));

        _factory.IdentityServiceMock
            .Given(Request.Create().WithPath("/api/auth/some-protected-resource").UsingGet()
                .WithHeader("Authorization", $"Bearer {jwt}"))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK));

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = "test@example.com", password = "ValidPassword123!" });
        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        var sessionCookie = cookies!.First(c => c.StartsWith(SessionManagementMiddleware.SessionCookieName));

        // Act
        var protectedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/some-protected-resource");
        protectedRequest.Headers.Add("Cookie", sessionCookie);
        var protectedResponse = await _client.SendAsync(protectedRequest);

        // Assert
        protectedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static string CreateJwt(TimeSpan expiresIn)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = "a-super-secret-key-for-testing-purpose"u8.ToArray();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("sub", Guid.NewGuid().ToString()) }),
            Expires = DateTime.UtcNow.Add(expiresIn),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}