using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProperTea.Identity.Tests.Utility;

public class TestSchemeProvider : AuthenticationSchemeProvider
{
    public TestSchemeProvider(IOptions<AuthenticationOptions> options)
        : base(options)
    {
    }

    protected TestSchemeProvider(
        IOptions<AuthenticationOptions> options,
        IDictionary<string, AuthenticationScheme> schemes
    )
        : base(options, schemes)
    {
    }

    public override Task<AuthenticationScheme> GetSchemeAsync(string name)
    {
        if (name == IdentityConstants.ExternalScheme)
        {
            var scheme = new AuthenticationScheme(
                IdentityConstants.ExternalScheme,
                "Test External",
                typeof(TestExternalSchemeHandler)
            );
            return Task.FromResult(scheme);
        }

        return base.GetSchemeAsync(name)!;
    }
}

public class TestExternalSchemeOptions : AuthenticationSchemeOptions
{
}

public class TestExternalSchemeHandler : SignOutAuthenticationHandler<TestExternalSchemeOptions>
{
    public const string DefaultScheme = "test_external";
    public const string TestUserId = "test_user@external.com";

    public TestExternalSchemeHandler(IOptionsMonitor<TestExternalSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    public TestExternalSchemeHandler(IOptionsMonitor<TestExternalSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var properties = new AuthenticationProperties();
        properties.Items.Add("LoginProvider", DefaultScheme);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, TestUserId),
            new Claim(ClaimTypes.Email, TestUserId)
        };
        var identity = new ClaimsIdentity(claims, DefaultScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, properties, DefaultScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }


    protected override async Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
    }
}